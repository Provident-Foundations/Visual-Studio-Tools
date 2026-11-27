using CommunityToolkit.Mvvm.Input;

namespace Visual.Studio.Toolset.UI.Controls.TextToHex;

/// <summary>
/// Provides the signature definition for a view model for the hexadecimal-to-text conversion control.
/// </summary>
public interface IHexToTextViewModel
{
    /// <summary>
    /// Gets the reference to the convert to text command.
    /// </summary>
    IRelayCommand? ConvertToTextCommand { get; }

    /// <summary>
    /// Gets the reference to the clear content command.
    /// </summary>
    IRelayCommand? ClearTextCommand { get; }

    /// <summary>
    /// Gets the reference to the copy content command.
    /// </summary>
    IRelayCommand? CopyTextCommand { get; }

    /// <summary>
    /// Gets the reference to the close content command.
    /// </summary>
    IRelayCommand? CloseTextCommand { get; }
}
