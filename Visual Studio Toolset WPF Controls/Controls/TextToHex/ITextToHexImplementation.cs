namespace Visual.Studio.Toolset.UI.Controls.TextToHex;

/// <summary>
/// Provides the signature definition for the a class to implement the command actions from
/// the <see cref="ITextToHexViewModel"/>.
/// 
/// </summary>
public interface ITextToHexImplementation
{
    /// <summary>
    /// Clears the current contents.
    /// </summary>
    void Clear();

    /// <summary>
    /// Indicates the parent or container is ready to be closed.
    /// </summary>
    void Close();

    /// <summary>
    /// Converts the provided hexadecimal value into a UTF-8 encoded string.
    /// </summary>
    /// <returns>
    /// <b>null</b> if the process is successful; otherwise, returns an error message.
    /// </returns>
    string? PerformConversion();

    /// <summary>
    /// Converts the current output text to the clipboard.
    /// </summary>
    void CopyOutputToClipboard();
}
