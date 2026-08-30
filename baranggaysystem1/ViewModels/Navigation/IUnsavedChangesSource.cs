namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Explicit dirty-state contract for fullscreen and embedded workflows.
/// </summary>
public interface IUnsavedChangesSource
{
	bool IsDirty { get; set; }
}
