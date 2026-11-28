namespace VS.Toolset.UI.SQL;

/// <summary>
/// Provides a container for representing a table selected by a user.
/// </summary>
/// <param name="schema">
/// A string specifying the database schema.
/// </param>
/// <param name="name">
/// A string specifying the table name.
/// </param>
public sealed class TableSelection(string  schema, string name)
{
    /// <summary>
    /// Gets the display name for the table selection, which is a combination of the schema and table name.
    /// </summary>
    /// <value>
    /// A string containing the display name value.
    /// </value>
    public string DisplayName => $"{Schema}.{Name}";

    /// <summary>
    /// Gets or sets a value indicating whether the table has been selected.
    /// </summary>
    /// <value>
    /// <b>true</b> if the table was selected; otherwise, <b>false</b>.
    /// </value>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets the database schema.
    /// </summary>
    /// <value>
    /// A string containing the schema name.
    /// </value>
    public string Schema { get; } = schema;

    /// <summary>
    /// Gets the table name.
    /// </summary>
    /// <value>
    /// A string containing the table name.
    /// </value>
    public string Name { get; } = name;
}
