#!/usr/bin/env python3
"""Build the bundled SQLite database from a MySQL full-backup SQL file."""

from __future__ import annotations

import argparse
import re
import sqlite3
from pathlib import Path
from typing import Any, Iterable


CREATE_RE = re.compile(
    r"CREATE TABLE(?: IF NOT EXISTS)?\s+`([^`]+)`\s*\((.*?)\)\s*ENGINE=.*?;",
    re.IGNORECASE | re.DOTALL,
)
INSERT_RE = re.compile(r"INSERT INTO `([^`]+)` VALUES\s+", re.IGNORECASE)
SYNC_STATUS_TABLES = (
    "expense_entry",
    "inventory_item",
    "asset_record",
    "procurement_request",
    "resident_classification",
)


def split_definition_lines(body: str) -> list[str]:
    result: list[str] = []
    buffer: list[str] = []
    depth = 0
    quoted = False
    for char in body:
        if char == "'" and (not buffer or buffer[-1] != "\\"):
            quoted = not quoted
        if not quoted:
            if char == "(":
                depth += 1
            elif char == ")":
                depth -= 1
            elif char == "," and depth == 0:
                result.append("".join(buffer).strip())
                buffer.clear()
                continue
        buffer.append(char)
    if buffer:
        result.append("".join(buffer).strip())
    return result


def sqlite_type(mysql_type: str) -> str:
    lowered = mysql_type.lower()
    if any(token in lowered for token in ("int", "bit", "bool")):
        return "INTEGER"
    if any(token in lowered for token in ("decimal", "numeric", "double", "float", "real")):
        return "REAL"
    if any(token in lowered for token in ("blob", "binary")):
        return "BLOB"
    return "TEXT"


def normalize_default(definition: str) -> str:
    match = re.search(
        r"\bDEFAULT\s+((?:'(?:\\.|[^'])*')|(?:CURRENT_TIMESTAMP)|(?:NULL)|(?:-?\d+(?:\.\d+)?))",
        definition,
        re.IGNORECASE,
    )
    if not match:
        return ""
    value = match.group(1)
    if value.upper() == "NULL":
        return ""
    value = value.replace("\\'", "''").replace("\\\\", "\\")
    return " DEFAULT " + value


def convert_create(table: str, body: str) -> tuple[str, list[tuple[str, list[str]]]]:
    definitions = split_definition_lines(body)
    primary_columns: list[str] = []
    unique_indexes: list[tuple[str, list[str]]] = []
    for definition in definitions:
        primary = re.match(r"PRIMARY KEY\s*\((.*?)\)", definition, re.IGNORECASE | re.DOTALL)
        if primary:
            primary_columns = re.findall(r"`([^`]+)`", primary.group(1))
            continue
        unique = re.match(r"UNIQUE KEY\s+`([^`]+)`\s*\((.*?)\)", definition, re.IGNORECASE | re.DOTALL)
        if unique:
            unique_indexes.append((unique.group(1), re.findall(r"`([^`]+)`", unique.group(2))))

    columns: list[str] = []
    inline_primary: str | None = None
    for definition in definitions:
        column = re.match(r"`([^`]+)`\s+([^\s,]+)(.*)", definition, re.DOTALL)
        if not column:
            continue
        name, raw_type, remainder = column.groups()
        sql_type = sqlite_type(raw_type)
        parts = [f'"{name}"', sql_type]
        auto_increment = "AUTO_INCREMENT" in remainder.upper()
        if auto_increment and primary_columns == [name] and sql_type == "INTEGER":
            parts.append("PRIMARY KEY AUTOINCREMENT")
            inline_primary = name
        elif "NOT NULL" in remainder.upper():
            parts.append("NOT NULL")
        parts.append(normalize_default(remainder))
        columns.append(" ".join(part for part in parts if part).strip())

    if primary_columns and inline_primary is None:
        columns.append("PRIMARY KEY (" + ", ".join(f'"{name}"' for name in primary_columns) + ")")
    create_sql = f'CREATE TABLE "{table}" (\n  ' + ",\n  ".join(columns) + "\n);"
    return create_sql, unique_indexes


def decode_mysql_string(text: str, index: int) -> tuple[str, int]:
    assert text[index] == "'"
    index += 1
    value: list[str] = []
    escapes = {"0": "\0", "b": "\b", "n": "\n", "r": "\r", "t": "\t", "Z": "\x1a"}
    while index < len(text):
        char = text[index]
        if char == "'":
            if index + 1 < len(text) and text[index + 1] == "'":
                value.append("'")
                index += 2
                continue
            return "".join(value), index + 1
        if char == "\\" and index + 1 < len(text):
            following = text[index + 1]
            value.append(escapes.get(following, following))
            index += 2
            continue
        value.append(char)
        index += 1
    raise ValueError("Unterminated MySQL string literal")


def parse_scalar(token: str) -> Any:
    value = token.strip()
    if not value or value.upper() == "NULL":
        return None
    if value.lower().startswith("0x"):
        return bytes.fromhex(value[2:])
    try:
        return int(value)
    except ValueError:
        try:
            return float(value)
        except ValueError:
            return value


def parse_rows(statement: str) -> list[tuple[Any, ...]]:
    rows: list[tuple[Any, ...]] = []
    index = 0
    while index < len(statement):
        while index < len(statement) and statement[index] in " \t\r\n,":
            index += 1
        if index >= len(statement) or statement[index] == ";":
            break
        if statement[index] != "(":
            raise ValueError(f"Expected row at offset {index}")
        index += 1
        row: list[Any] = []
        token: list[str] = []
        while index < len(statement):
            char = statement[index]
            if char == "'":
                if token and "".join(token).strip():
                    raise ValueError(f"Unexpected string at offset {index}")
                value, index = decode_mysql_string(statement, index)
                row.append(value)
                while index < len(statement) and statement[index].isspace():
                    index += 1
                if index < len(statement) and statement[index] == ",":
                    index += 1
                    continue
                if index < len(statement) and statement[index] == ")":
                    index += 1
                    break
                continue
            if char == ",":
                row.append(parse_scalar("".join(token)))
                token.clear()
                index += 1
                continue
            if char == ")":
                row.append(parse_scalar("".join(token)))
                token.clear()
                index += 1
                break
            token.append(char)
            index += 1
        rows.append(tuple(row))
    return rows


def iter_insert_statements(sql: str) -> Iterable[tuple[str, str]]:
    for match in INSERT_RE.finditer(sql):
        index = match.end()
        quoted = False
        escaped = False
        while index < len(sql):
            char = sql[index]
            if quoted:
                if escaped:
                    escaped = False
                elif char == "\\":
                    escaped = True
                elif char == "'":
                    quoted = False
            elif char == "'":
                quoted = True
            elif char == ";":
                yield match.group(1), sql[match.end() : index + 1]
                break
            index += 1


def build_database(source: Path, destination: Path) -> None:
    sql = source.read_text(encoding="utf-8", errors="replace")
    if destination.exists():
        destination.unlink()
    destination.parent.mkdir(parents=True, exist_ok=True)
    connection = sqlite3.connect(destination)
    try:
        connection.execute("PRAGMA foreign_keys=OFF")
        index_definitions: list[tuple[str, str, list[str]]] = []
        for match in CREATE_RE.finditer(sql):
            table = match.group(1)
            create_sql, unique_indexes = convert_create(table, match.group(2))
            connection.execute(create_sql)
            index_definitions.extend((table, name, columns) for name, columns in unique_indexes)

        imported = 0
        for table, values in iter_insert_statements(sql):
            rows = parse_rows(values)
            if not rows:
                continue
            column_count = len(connection.execute(f'PRAGMA table_info("{table}")').fetchall())
            for row in rows:
                if len(row) != column_count:
                    raise ValueError(f"{table}: backup row has {len(row)} values; schema has {column_count} columns")
            placeholders = ", ".join("?" for _ in range(column_count))
            connection.executemany(f'INSERT INTO "{table}" VALUES ({placeholders})', rows)
            imported += len(rows)

        for table, name, columns in index_definitions:
            if not columns:
                continue
            quoted = ", ".join(f'"{column}"' for column in columns)
            connection.execute(f'CREATE UNIQUE INDEX IF NOT EXISTS "{name}" ON "{table}" ({quoted})')

        connection.execute(
            """CREATE TABLE IF NOT EXISTS payment_void (
                   void_id INTEGER PRIMARY KEY AUTOINCREMENT,
                   barangay_id INTEGER NOT NULL DEFAULT 1,
                   payment_source TEXT NOT NULL,
                   payment_id INTEGER NOT NULL,
                   void_reason TEXT NOT NULL,
                   voided_by_user_id INTEGER,
                   voided_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                   UNIQUE (barangay_id, payment_source, payment_id)
               )"""
        )
        connection.execute("CREATE INDEX IF NOT EXISTS idx_payment_void_date ON payment_void (voided_at)")
        for table in SYNC_STATUS_TABLES:
            existing = {row[1] for row in connection.execute(f'PRAGMA table_info("{table}")')}
            if "sync_status" not in existing:
                connection.execute(
                    f'ALTER TABLE "{table}" ADD COLUMN sync_status TEXT NOT NULL DEFAULT \'synced\''
                )
        for column, definition in (
            ("before_data", "TEXT"),
            ("after_data", "TEXT"),
            ("remarks", "TEXT"),
            ("user_id", "INTEGER"),
            ("created_at", "TEXT"),
        ):
            existing = {row[1] for row in connection.execute('PRAGMA table_info("audit_trail")')}
            if column not in existing:
                connection.execute(f'ALTER TABLE "audit_trail" ADD COLUMN "{column}" {definition}')
        connection.execute("UPDATE audit_trail SET created_at = COALESCE(created_at, action_at)")
        connection.executemany(
            "INSERT OR IGNORE INTO schema_migrations (migration_name) VALUES (?)",
            [
                ("20260309_seed_30_records_30_transactions_reports.sql",),
                ("20260428_ph_public_reference_seed.sql",),
                ("20260729_workflow_integrity.sql",),
            ],
        )
        connection.execute("PRAGMA user_version=20260729")
        connection.commit()
        check = connection.execute("PRAGMA integrity_check").fetchone()[0]
        if check != "ok":
            raise RuntimeError(f"SQLite integrity check failed: {check}")
        table_count = connection.execute(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
        ).fetchone()[0]
        print(f"Created {destination} with {table_count} tables and {imported} imported rows.")
    finally:
        connection.close()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("mysql_backup", type=Path)
    parser.add_argument("sqlite_database", type=Path)
    args = parser.parse_args()
    build_database(args.mysql_backup, args.sqlite_database)


if __name__ == "__main__":
    main()
