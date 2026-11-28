using Microsoft.Data.SqlClient;
using Provident.Foundations.Wpf.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace VS.Toolset.UI.Dialogs;

/// <summary>Collects and verifies SQL Server connection settings.</summary>
public partial class SqlConnectionWindow : ProvidentWindowBase
{
    private readonly ISqlConnectionService _service;
    private CancellationTokenSource? _operation;
    private bool _closed;

    /// <summary>Initializes a connection dialog with the standard SQL Server service.</summary>
    public SqlConnectionWindow() : this(new SqlConnectionService()) { }

    /// <summary>Initializes a connection dialog with a supplied connection service.</summary>
    public SqlConnectionWindow(ISqlConnectionService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        InitializeComponent();
        ConnectedAs.Text = $"{Environment.UserDomainName}\\{Environment.UserName}";
        Loaded += (_, _) => ServerText.Focus();
    }

    /// <summary>Gets the verified settings after a successful Connect operation.</summary>
    public SqlConnectionResult? Result { get; private set; }

    private void AuthenticationChanged(object sender, RoutedEventArgs e)
    {
        bool useSqlAuthentication = SqlAuthentication?.IsChecked == true;
        if (CredentialsPanel is not null)
            CredentialsPanel.Visibility = useSqlAuthentication ? Visibility.Visible : Visibility.Collapsed;
        if (ConnectedAsPanel is not null)
            ConnectedAsPanel.Visibility = useSqlAuthentication ? Visibility.Collapsed : Visibility.Visible;
    }

    private SqlConnectionStringBuilder CreateBuilder()
    {
        if (string.IsNullOrWhiteSpace(ServerText.Text))
            throw new ConnectionValidationException("Enter a server or instance.");
        bool useSqlAuthentication = SqlAuthentication.IsChecked == true;
        if (useSqlAuthentication && string.IsNullOrWhiteSpace(UserName.Text))
            throw new ConnectionValidationException("Enter a SQL Server user name.");
        if (useSqlAuthentication && Password.Password.Length == 0)
            throw new ConnectionValidationException("Enter a SQL Server password.");

        int timeout = ParseNumber(Timeout.Text, "Connection timeout", 1, int.MaxValue);
        int minPool = ParseNumber(MinPool.Text, "Minimum pool size", 0, int.MaxValue);
        int maxPool = ParseNumber(MaxPool.Text, "Maximum pool size", 1, int.MaxValue);
        if (minPool > maxPool)
            throw new ConnectionValidationException("Minimum pool size cannot exceed maximum pool size.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = ServerText.Text.Trim(),
            InitialCatalog = Database.Text.Trim(),
            IntegratedSecurity = !useSqlAuthentication,
            Encrypt = Encrypt.IsChecked == true,
            TrustServerCertificate = TrustCertificate.IsChecked == true,
            Pooling = Pooling.IsChecked == true,
            MultipleActiveResultSets = Mars.IsChecked == true,
            Enlist = Enlist.IsChecked == true,
            ConnectTimeout = timeout,
            MinPoolSize = minPool,
            MaxPoolSize = maxPool,
            ConnectRetryCount = ParseNumber(RetryCount.Text, "Retry count", 0, 255),
            ConnectRetryInterval = ParseNumber(RetryInterval.Text, "Retry interval", 1, 60),
            PacketSize = ParseNumber(PacketSize.Text, "Packet size", 512, 32768),
            PersistSecurityInfo = false
        };
        if (useSqlAuthentication)
        {
            builder.UserID = UserName.Text.Trim();
            builder.Password = Password.Password;
        }
        return builder;
    }

    private static int ParseNumber(string text, string name, int min, int max)
    {
        if (!int.TryParse(text, out int value) || value < min || value > max)
            throw new ConnectionValidationException($"{name} must be between {min} and {max}.");
        return value;
    }

    private async void TestClick(object sender, RoutedEventArgs e) => await RunAsync(Operation.Test);
    private async void ConnectClick(object sender, RoutedEventArgs e) => await RunAsync(Operation.Connect);
    private async void BrowseClick(object sender, RoutedEventArgs e) => await RunAsync(Operation.Browse);

    private async Task RunAsync(Operation operation)
    {
        if (_operation is not null)
            return;
        SqlConnectionStringBuilder builder;
        try
        {
            builder = CreateBuilder();
        }
        catch (ConnectionValidationException ex)
        {
            StatusText.Text = ex.Message;
            return;
        }
        catch (ArgumentException)
        {
            StatusText.Text = "The connection settings are invalid. Check the server and advanced options.";
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _operation = cancellation;
        SettingsPanel.IsEnabled = false;
        TestButton.IsEnabled = false;
        ConnectButton.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;
        CancelButton.Content = "Cancel operation";
        StatusText.Text = operation == Operation.Browse ? "Loading databases..." : "Connecting...";
        try
        {
            if (operation == Operation.Browse)
            {
                var databases = await _service.GetDatabasesAsync(builder.ConnectionString, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (_closed) return;
                string selected = Database.Text;
                Database.ItemsSource = databases;
                Database.Text = selected;
                StatusText.Text = databases.Count == 0
                    ? "No accessible databases were found. You can enter a database name manually."
                    : "Select a database or enter its name.";
            }
            else
            {
                await _service.TestAsync(builder.ConnectionString, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (_closed) return;
                if (operation == Operation.Connect)
                {
                    Result = new SqlConnectionResult(builder.ConnectionString);
                    DialogResult = true;
                }
                else
                    StatusText.Text = "Connection successful.";
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (!_closed) StatusText.Text = "Operation canceled.";
        }
        catch (SqlException ex)
        {
            if (!_closed)
            {
                string guidance = ex.Number switch
                {
                    18456 => "Check the user name and password or Windows account permissions.",
                    4060 => "Check the database name and access permissions.",
                    -2 => "The connection timed out. Check the server and network.",
                    _ => "Check the server, network, credentials, database access and certificate settings."
                };
                StatusText.Text = $"SQL Server operation failed (error {ex.Number}). {guidance}";
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or TimeoutException)
        {
            if (!_closed) StatusText.Text = "The connection could not be completed. Check the connection settings and try again.";
        }
        finally
        {
            _operation = null;
            if (!_closed)
            {
                SettingsPanel.IsEnabled = true;
                TestButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                Progress.Visibility = Visibility.Collapsed;
                CancelButton.Content = "Cancel";
            }
        }
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        if (_operation is not null)
            _operation.Cancel();
        else
            Close();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelClick(this, new RoutedEventArgs());
            e.Handled = true;
        }
        base.OnPreviewKeyDown(e);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _closed = true;
        _operation?.Cancel();
        Password.Clear();
    }

    private sealed class ConnectionValidationException(string message) : ArgumentException(message);

    private enum Operation { Test, Connect, Browse }
}
