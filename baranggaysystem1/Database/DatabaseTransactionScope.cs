using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

/// <summary>
/// Provider-neutral transaction used by multi-step business commands.
/// Supports MySQL/Hostinger and the selected SQLite local file.
/// </summary>
internal sealed class DatabaseTransactionScope : IAsyncDisposable, IDisposable
{
	private readonly DbConnection _connection;
	private readonly DbTransaction _transaction;
	private readonly bool _sqlite;
	private readonly List<QueuedMutation> _mutations = new();
	private bool _completed;

	private DatabaseTransactionScope(DbConnection connection, DbTransaction transaction, bool sqlite)
	{
		_connection = connection;
		_transaction = transaction;
		_sqlite = sqlite;
	}

	public static async Task<DatabaseTransactionScope> BeginAsync(CancellationToken cancellationToken = default)
	{
		bool sqlite = OfflineDatabaseSupport.IsOffline || DBConnection.IsSqliteSelected();
		DbConnection connection = sqlite
			? OfflineDatabaseSupport.GetConnection()
			: DBConnection.GetConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
		}
		DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
		return new DatabaseTransactionScope(connection, transaction, sqlite);
	}

	public async Task<int> ExecuteNonQueryAsync(
		string sql,
		IReadOnlyDictionary<string, object?>? parameters = null,
		CancellationToken cancellationToken = default)
	{
		await using DbCommand command = CreateCommand(sql, parameters);
		int affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		TrackMutation(sql, parameters);
		return affected;
	}

	public async Task<T?> ExecuteScalarAsync<T>(
		string sql,
		IReadOnlyDictionary<string, object?>? parameters = null,
		CancellationToken cancellationToken = default)
	{
		await using DbCommand command = CreateCommand(sql, parameters);
		object? value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
		if (value == null || value == DBNull.Value)
		{
			return default;
		}
		return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
	}

	public async Task<DataTable> LoadTableAsync(
		string sql,
		IReadOnlyDictionary<string, object?>? parameters = null,
		CancellationToken cancellationToken = default)
	{
		await using DbCommand command = CreateCommand(sql, parameters);
		await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
		DataTable table = new();
		table.Load(reader);
		return table;
	}

	public async Task<long> ExecuteInsertAsync(
		string sql,
		IReadOnlyDictionary<string, object?>? parameters = null,
		CancellationToken cancellationToken = default)
	{
		await using DbCommand command = CreateCommand(sql, parameters);
		await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		TrackMutation(sql, parameters);
		if (command is MySqlCommand mySqlCommand)
		{
			return mySqlCommand.LastInsertedId;
		}
		await using DbCommand idCommand = _connection.CreateCommand();
		idCommand.Transaction = _transaction;
		idCommand.CommandText = "SELECT last_insert_rowid();";
		object? value = await idCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
		return Convert.ToInt64(value ?? 0, CultureInfo.InvariantCulture);
	}

	public async Task CommitAsync(CancellationToken cancellationToken = default)
	{
		if (_completed)
		{
			throw new InvalidOperationException("This database transaction has already completed.");
		}
		await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
		_completed = true;
		if (_sqlite && OfflineDatabaseSupport.IsOffline && !DBConnection.IsSqliteSelected())
		{
			foreach (QueuedMutation mutation in _mutations)
			{
				OfflineSyncService.QueueChange(mutation.Sql, mutation.Parameters);
			}
		}
	}

	public async Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		if (_completed)
		{
			return;
		}
		await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
		_completed = true;
	}

	private DbCommand CreateCommand(string sql, IReadOnlyDictionary<string, object?>? parameters)
	{
		DbCommand command = _connection.CreateCommand();
		command.Transaction = _transaction;
		command.CommandText = _sqlite ? OfflineSqlCompat.NormalizeSql(sql) : sql;
		command.CommandTimeout = 30;
		if (parameters != null)
		{
			foreach ((string name, object? value) in parameters)
			{
				DbParameter parameter = command.CreateParameter();
				parameter.ParameterName = name;
				parameter.Value = value ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}
		return command;
	}

	private void TrackMutation(string sql, IReadOnlyDictionary<string, object?>? parameters)
	{
		if (!_sqlite || !IsMutation(sql))
		{
			return;
		}
		List<MySqlParameter> snapshot = new();
		if (parameters != null)
		{
			foreach ((string name, object? value) in parameters)
			{
				snapshot.Add(new MySqlParameter(name, value ?? DBNull.Value));
			}
		}
		_mutations.Add(new QueuedMutation(sql, snapshot));
	}

	private static bool IsMutation(string sql)
	{
		string value = (sql ?? string.Empty).TrimStart();
		return value.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase);
	}

	public void Dispose()
	{
		if (!_completed)
		{
			try
			{
				_transaction.Rollback();
			}
			catch
			{
			}
		}
		_transaction.Dispose();
		_connection.Dispose();
		_completed = true;
	}

	public async ValueTask DisposeAsync()
	{
		if (!_completed)
		{
			try
			{
				await _transaction.RollbackAsync().ConfigureAwait(false);
			}
			catch
			{
			}
		}
		await _transaction.DisposeAsync().ConfigureAwait(false);
		await _connection.DisposeAsync().ConfigureAwait(false);
		_completed = true;
	}

	private sealed record QueuedMutation(string Sql, IReadOnlyList<MySqlParameter> Parameters);
}
