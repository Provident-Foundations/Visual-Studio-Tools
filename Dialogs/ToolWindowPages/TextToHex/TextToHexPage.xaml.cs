using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace VS.Toolset.UI.Dialogs.ToolWindowPages.TextToHex;

    /// <summary>
    /// Interaction logic for TextToHexPage.xaml
    /// </summary>
    public partial class TextToHexPage : Page
    {
        public TextToHexPage()
        {
            InitializeComponent();
            DataContext = new TextToHexViewModel(CopyToClipboard, ShowError);
        }

        public event EventHandler? CloseRequested;

        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (ExternalException exception)
            {
                ShowError($"The text could not be copied to the clipboard.\n\n{exception.Message}");
            }
        }

        private void ShowError(string message)
        {
            Window? owner = Window.GetWindow(this);
            if (owner is null)
                MessageBox.Show(message, "Text to hexadecimal", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(owner, message, "Text to hexadecimal", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void HandleCloseClicked(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
