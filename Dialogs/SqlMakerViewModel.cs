using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Provident.Foundations.SqlMaker;
using System.IO;
using VS.Toolset.UI.SQL;

namespace VS.Toolset.UI.Dialogs;

/// <summary>
/// Holds the SQL Maker dialog state and coordinates loading and saving a project.
/// </summary>
public sealed class SqlMakerViewModel : ObservableObject
{
    private readonly Func<string?> _configureConnection;
    private readonly Func<bool> _confirmConnectionChange;
    private readonly Func<bool> _confirmTableReload;
    private readonly Func<string?> _getManualConnectionString;
    private readonly Action _clearManualConnectionString;
    private readonly Func<string?> _chooseSavePath;
    private readonly Action<string, string> _showError;
    private readonly Action<string, string> _showInformation;
    private readonly Action _closeWindow;
    private bool _isLoading;
    private string? _loadedConnection;
    private string? _verifiedConnection;
    private string _connectionSummary = "No verified connection";
    private string _projectName = "Database Project 1";
    private string _description = "Default Database Project";
    private string _dataAccessNamespace = string.Empty;
    private string _entityNamespace = string.Empty;
    private string _procedurePrefix = string.Empty;
    private string _procedureSuffix = string.Empty;
    private bool _generateJoins;
    private string _joinPrefix = string.Empty;
    private string _joinSuffix = string.Empty;
    private string _idColumn = string.Empty;
    private bool _softDelete;
    private string _softDeleteColumn = string.Empty;
    private bool _useRegions;
    private string _standardNamespaces = string.Empty;

    public SqlMakerViewModel(
        Func<string?> configureConnection,
        Func<bool> confirmConnectionChange,
        Func<bool> confirmTableReload,
        Func<string?> getManualConnectionString,
        Action clearManualConnectionString,
        Func<string?> chooseSavePath,
        Action<string, string> showError,
        Action<string, string> showInformation,
        Action closeWindow)
    {
        _configureConnection = configureConnection;
        _confirmConnectionChange = confirmConnectionChange;
        _confirmTableReload = confirmTableReload;
        _getManualConnectionString = getManualConnectionString;
        _clearManualConnectionString = clearManualConnectionString;
        _chooseSavePath = chooseSavePath;
        _showError = showError;
        _showInformation = showInformation;
        _closeWindow = closeWindow;

        ConfigureConnectionCommand = new RelayCommand(ConfigureConnection, () => !IsLoading);
        LoadTablesCommand = new AsyncRelayCommand(LoadTablesAsync, () => !IsLoading);
        SaveCommand = new RelayCommand(Save, () => !IsLoading);
    }

    public TableSelectionCollection Tables { get; } = [];

    public IRelayCommand ConfigureConnectionCommand { get; }
    public IAsyncRelayCommand LoadTablesCommand { get; }
    public IRelayCommand SaveCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsNotLoading));
                ConfigureConnectionCommand.NotifyCanExecuteChanged();
                LoadTablesCommand.NotifyCanExecuteChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsNotLoading => !IsLoading;

    public string ConnectionSummary
    {
        get => _connectionSummary;
        private set => SetProperty(ref _connectionSummary, value);
    }

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string DataAccessNamespace
    {
        get => _dataAccessNamespace;
        set => SetProperty(ref _dataAccessNamespace, value);
    }

    public string EntityNamespace
    {
        get => _entityNamespace;
        set => SetProperty(ref _entityNamespace, value);
    }

    public string ProcedurePrefix
    {
        get => _procedurePrefix;
        set => SetProperty(ref _procedurePrefix, value);
    }

    public string ProcedureSuffix
    {
        get => _procedureSuffix;
        set => SetProperty(ref _procedureSuffix, value);
    }

    public bool GenerateJoins
    {
        get => _generateJoins;
        set => SetProperty(ref _generateJoins, value);
    }

    public string JoinPrefix
    {
        get => _joinPrefix;
        set => SetProperty(ref _joinPrefix, value);
    }

    public string JoinSuffix
    {
        get => _joinSuffix;
        set => SetProperty(ref _joinSuffix, value);
    }

    public string IdColumn
    {
        get => _idColumn;
        set => SetProperty(ref _idColumn, value);
    }

    public bool SoftDelete
    {
        get => _softDelete;
        set => SetProperty(ref _softDelete, value);
    }

    public string SoftDeleteColumn
    {
        get => _softDeleteColumn;
        set => SetProperty(ref _softDeleteColumn, value);
    }

    public bool UseRegions
    {
        get => _useRegions;
        set => SetProperty(ref _useRegions, value);
    }

    public string StandardNamespaces
    {
        get => _standardNamespaces;
        set => SetProperty(ref _standardNamespaces, value);
    }

    public void UpdateConnectionSummary(string? manualConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(manualConnectionString))
        {
            ConnectionSummary = "Using an unverified connection string.";
        }
        else if (_verifiedConnection is null)
        {
            ConnectionSummary = "No verified connection";
        }
        else
        {
            var builder = new SqlConnectionStringBuilder(_verifiedConnection);
            ConnectionSummary = string.IsNullOrWhiteSpace(builder.InitialCatalog)
                ? $"Verified: {builder.DataSource} (default database)"
                : $"Verified: {builder.DataSource} / {builder.InitialCatalog}";
        }
    }

    public bool OnWindowClosing()
    {
        if (IsLoading)
        {
            _showInformation("Wait for the table list to finish loading.", "SQL Maker");
            return false;
        }

        _verifiedConnection = null;
        _loadedConnection = null;
        return true;
    }

    private void ConfigureConnection()
    {
        string? value = _configureConnection();
        if (value is null)
        {
            return;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(value);
            bool changed = !string.Equals(_loadedConnection, builder.ConnectionString, StringComparison.Ordinal);
            if (!changed || (Tables.Any(table => table.IsSelected) && !_confirmConnectionChange()))
            {
                return;
            }

            _verifiedConnection = builder.ConnectionString;
            _clearManualConnectionString();
            UpdateConnectionSummary(null);
            Tables.Clear();
            _loadedConnection = null;
        }
        catch (ArgumentException exception)
        {
            _showError(exception.Message, "Unable to configure connection");
        }
    }

    private async Task LoadTablesAsync()
    {
        if (Tables.Any(table => table.IsSelected) && !_confirmTableReload())
        {
            return;
        }

        try
        {
            SqlConnectionStringBuilder connection = GetConnection(_getManualConnectionString());
            IsLoading = true;
            var tables = await SqlMakerTableDiscovery.GetTablesAsync(connection.ConnectionString);
            Tables.Clear();
            foreach (var table in tables)
            {
                Tables.Add(new TableSelection(table.Schema, table.Name));
            }

            _loadedConnection = connection.ConnectionString;
        }
        catch (SqlException exception)
        {
            _showError($"Unable to load tables (SQL Server error {exception.Number}). Check the server, credentials, database access and certificate settings.", "Unable to load tables");
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            _showError("Unable to load tables. Check the connection settings and try again.", "Unable to load tables");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            _showError("Enter a project name.", "Unable to save SQL Maker project");
            return;
        }

        SqlConnectionStringBuilder connection;
        try
        {
            connection = GetConnection(_getManualConnectionString());
            var selected = Tables.Where(table => table.IsSelected).ToList();
            if (selected.Count > 0 && !string.Equals(_loadedConnection, connection.ConnectionString, StringComparison.Ordinal))
            {
                throw new ArgumentException("The connection has changed. Reload tables before saving.");
            }

            if (selected.GroupBy(table => table.Name, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                throw new ArgumentException("Tables with the same name in different schemas cannot be included together in this project format.");
            }

            if (SoftDelete && string.IsNullOrWhiteSpace(SoftDeleteColumn))
            {
                throw new ArgumentException("Enter the soft delete column name.");
            }

            string? fileName = _chooseSavePath();
            if (fileName is null)
            {
                return;
            }

            connection.Remove("Password");
            connection.PersistSecurityInfo = false;
            using var project = new SqlDataProject { FileName = fileName, ProjectName = ProjectName.Trim(), Description = Description };
            project.SetDefaultDataSourceConnectionString(connection.ConnectionString);
            var options = project.Options ?? throw new InvalidOperationException("Project options are unavailable.");
            options.DataAccessNamespace = DataAccessNamespace;
            options.EntityNamespace = EntityNamespace;
            options.StoredProcedureNamePrefix = ProcedurePrefix;
            options.StoredProcedureNameSuffix = ProcedureSuffix;
            options.GenerateJoins = GenerateJoins;
            options.SubJoinObjectNamePrefix = JoinPrefix;
            options.SubJoinObjectNameSuffix = JoinSuffix;
            options.DefaultIdField = IdColumn;
            options.DefaultSoftDelete = SoftDelete;
            options.DefaultSoftDeleteField = SoftDeleteColumn;
            options.UseRegions = UseRegions;
            foreach (string value in StandardNamespaces.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                options.StandardNamespaces!.Add(value);
            }

            var projectTables = project.Tables ?? throw new InvalidOperationException("Project tables are unavailable.");
            foreach (var choice in selected)
            {
                var table = new SqlDataProjectTable { Name = choice.Name, Qualifier = choice.Schema };
                if (table.Profile is not null)
                {
                    table.Profile.TableName = choice.Name;
                    table.Profile.QualifiedName = $"[{choice.Schema.Replace("]", "]]", StringComparison.Ordinal)}].[{choice.Name.Replace("]", "]]", StringComparison.Ordinal)}]";
                }

                projectTables.Add(table);
            }

            if (!project.Save())
            {
                throw new IOException(string.IsNullOrWhiteSpace(project.ExceptionMessages) ? "The project could not be saved." : project.ExceptionMessages);
            }

            _closeWindow();
        }
        catch (ArgumentException exception)
        {
            _showError(exception.Message, "Unable to save SQL Maker project");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _showError(exception.Message, "Unable to save SQL Maker project");
        }
    }

    private SqlConnectionStringBuilder GetConnection(string? manualConnectionString)
    {
        string? value = string.IsNullOrWhiteSpace(manualConnectionString) ? _verifiedConnection : manualConnectionString;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Configure a SQL Server connection or enter a connection string.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(value);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException("The connection string is invalid. Check its syntax and supported options.");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new ArgumentException("Specify a server and database in the connection string.");
        }

        return builder;
    }
}

