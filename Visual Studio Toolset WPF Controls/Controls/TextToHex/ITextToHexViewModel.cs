using CommunityToolkit.Mvvm.Input;

namespace Visual.Studio.Toolset.UI.Controls.TextToHex;

/// <summary>
/// Provides the signature definition for a view model for the text-to-hexadecimal conversion control.
/// </summary>
public interface ITextToHexViewModel
{
    /// <summary>
    /// Gets the reference to the convert to hexadecimal command.
    /// </summary>
    IRelayCommand? ConvertToHexCommand { get; }

    /// <summary>
    /// Gets the reference to the clear content command.
    /// </summary>
    IRelayCommand? ClearHexCommand { get; }

    /// <summary>
    /// Gets the reference to the copy content command.
    /// </summary>
    IRelayCommand? CopyHexCommand { get; }

    /// <summary>
    /// Gets the reference to the close content command.
    /// </summary>
    IRelayCommand? CloseHexCommand { get; }
}
