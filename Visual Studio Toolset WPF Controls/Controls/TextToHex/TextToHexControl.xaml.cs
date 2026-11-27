using Provident.Foundations.Wpf.Controls;
using Visual.Studio.Toolset.UI.Controls.TextToHex;

namespace VS.Toolset.UI.Controls.TextToHex
{
    /// <summary>
    /// Interaction logic for TextToHexControl.xaml
    /// </summary>
    public partial class TextToHexControl : ProvidentUserControlBase, ITextToHexImplementation
    {
        #region Public Events
        /// <summary>
        /// Raises an event to the parent container requesting the current instance be closed.
        /// </summary>
        public event EventHandler? CloseRequest;
        #endregion

        #region Constructor / Dispose Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="TextToHexControl"/> class.
        /// </summary>
        public TextToHexControl() : base()
        {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        #endregion

        #region ITextToHexImplementation Methods for Related Commands
        /// <summary>
        /// Clears the current contents.
        /// </summary>
        public void Clear()
        {
            OriginalText.Text = string.Empty;
            OutputText.Text = string.Empty;
        }

        /// <summary>
        /// Raises the request to close the control.
        /// </summary>
        public void Close()
        {
            OnCloseRequest(EventArgs.Empty);
        }

        /// <summary>
        /// Performs the conversion of the original text to hexadecimal UTF-8 bytes.
        /// </summary>
        /// <returns>
        /// <b>null</b> if the conversion failed; otherwise, the converted hexadecimal UTF-8 bytes.
        /// </returns>
        public string? PerformConversion()
        {
            string? message = null;

            if (!string.IsNullOrEmpty(OriginalText.Text))
            {
                byte[]? content;
                message = TryConvert(OriginalText.Text, out content );
                if (message is null && content is not null)
                {
                    message = TryEncode(content, out string? encodedString);
                    if (message is null && encodedString is not null)
                    {
                        OutputText.Text = encodedString;
                    }
                }
            }
            return message;
        }

        /// <summary>
        /// Copies the content of the output text box to the clipboard.
        /// </summary>
        public void CopyOutputToClipboard()
        {
            try
            {
                System.Windows.Clipboard.SetText(OutputText.Text);
            }
            catch
            {
            }
        }
        #endregion

        #region Protected Event Methods
        /// <summary>
        /// Raises the <see cref="CloseRequest"/> event.
        /// </summary>
        /// <param name="e">
        /// An <see cref="EventArgs"/> that contains the event data.
        /// </param>
        private void OnCloseRequest(EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CloseRequest?.Invoke(this, e);
            });
        }
        #endregion

        #region Private Methods / Functions
        private string? TryConvert(string? original, out byte[]? content)
        {
            string? message = null;
            try
            {
                content = Convert.FromHexString(OriginalText.Text);
            }
            catch(Exception ex)
            {
                content = null;
                message = ex.Message;
            }
            return message;
        }
        private string? TryEncode(byte[]? content, out string? encodedData)
        {
            string? message = null;
            try
            {
                encodedData = System.Text.Encoding.UTF8.GetString(content);
            }
            catch(Exception ex)
            {
                message = ex.Message;
                encodedData = null;
            }
            return message;
        }
        #endregion
    }
}
