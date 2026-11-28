using Microsoft.Win32;
using Provident.Foundations.Wpf.Controls;
using System.ComponentModel;
using System.Windows;

namespace VS.Toolset.UI.Dialogs;

/// <summary>
/// Provides the view for creating a new SQL Maker project.
/// </summary>
public partial class SqlMakerWindow : ProvidentWindowBase
{
    private readonly SqlMakerViewModel _vm;

    public SqlMakerWindow()
    {
        InitializeComponent();
        _vm = new SqlMakerViewModel(
            ConfigureConnection,
            ConfirmConnectionChange,
            ConfirmTableReload,
            () => ConnectionString.Password,
            () => ConnectionString.Clear(),
            ChooseSavePath,
            ShowError,
            ShowInformation,
            () => DialogResult = true);
        DataContext = _vm;
    }

    private string? ConfigureConnection()
    {
        var dialog = new SqlConnectionWindow { Owner = this };
        return dialog.ShowDialog() == true ? dialog.Result?.ConnectionString : null;
    }

    private bool ConfirmConnectionChange() =>
        MessageBox.Show(this, "Changing the connection clears the current table selection. Continue?",
            "SQL Maker", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private bool ConfirmTableReload() =>
        MessageBox.Show(this, "Reloading tables clears the current selection. Continue?",
            "SQL Maker", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private string? ChooseSavePath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "SQL Maker projects (*.adbx)|*.adbx|All files (*.*)|*.*",
            DefaultExt = ".adbx",
            AddExtension = true,
            FileName = "DatabaseProject.adbx"
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void ConnectionStringChanged(object sender, RoutedEventArgs e) =>
        _vm.UpdateConnectionSummary(ConnectionString.Password);

    private void ShowError(string message, string title) =>
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private void ShowInformation(string message, string title) =>
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_vm.OnWindowClosing();
        if (!e.Cancel)
        {
            ConnectionString.Clear();
        }
    }
}
