using Xceed.Wpf.AvalonDock.Layout;

namespace VS.Toolset.UI;

/// <summary>
/// Controls the visibility and activation of docked tool windows.
/// </summary>
internal sealed class ToolWindowController
{
    /// <summary>
    /// Shows a pane and restores it from auto-hide if necessary.
    /// </summary>
    /// <param name="pane">
    /// The pane to show.
    /// </param>
    /// <param name="activate">
    /// Whether to activate the pane after showing it.
    /// </param>
    public void Show(LayoutAnchorable pane, bool activate = false)
    {
        if (pane.IsHidden)
            pane.Show();
        if (pane.IsAutoHidden)
            pane.ToggleAutoHide();
        if (activate)
            pane.IsActive = true;
    }

    /// <summary>
    /// Shows or hides a pane according to the requested state.
    /// </summary>
    /// <param name="pane">
    /// The pane to update.
    /// </param>
    /// <param name="isVisible">
    /// <see langword="true"/> to show the pane; otherwise, <see langword="false"/>.
    /// </param>
    public void SetVisibility(LayoutAnchorable pane, bool isVisible)
    {
        if (isVisible)
            pane.Show();
        else
            pane.Hide();
    }
}
