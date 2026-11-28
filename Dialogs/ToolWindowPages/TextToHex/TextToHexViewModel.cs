using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;

namespace VS.Toolset.UI.Dialogs.ToolWindowPages.TextToHex;

internal sealed class TextToHexViewModel : ObservableObject
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly Action<string> _copy;
    private readonly Action<string> _showError;
    private string _input = string.Empty;
    private string _output = string.Empty;

    public TextToHexViewModel(Action<string> copy, Action<string> showError)
    {
        _copy = copy;
        _showError = showError;
        ConvertCommand = new RelayCommand(Convert, () => Input.Length > 0);
        ClearCommand = new RelayCommand(Clear, () => Input.Length > 0 || Output.Length > 0);
        CopyCommand = new RelayCommand(() => _copy(Output), () => Output.Length > 0);
    }

    public string Input
    {
        get => _input;
        set
        {
            if (SetProperty(ref _input, value ?? string.Empty))
            {
                Output = string.Empty;
                ConvertCommand.NotifyCanExecuteChanged();
                ClearCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Output
    {
        get => _output;
        set
        {
            if (SetProperty(ref _output, value ?? string.Empty))
            {
                CopyCommand.NotifyCanExecuteChanged();
                ClearCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IRelayCommand ConvertCommand { get; }
    public IRelayCommand ClearCommand { get; }
    public IRelayCommand CopyCommand { get; }

    private void Convert()
    {
        try
        {
            Output = System.Convert.ToHexString(Utf8.GetBytes(Input));
        }
        catch (EncoderFallbackException)
        {
            Output = string.Empty;
            _showError("The input contains invalid Unicode text and cannot be converted to UTF-8.");
        }
    }

    private void Clear()
    {
        Input = string.Empty;
        Output = string.Empty;
    }
}
