namespace ModelBuilder;

public class DbModel
{
    public string Name { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string ExcludedColumns { get; set; } = string.Empty;
    public string ExcludedTables { get; set; } = string.Empty;
    public string ModelDefinition { get; set; } = string.Empty;
}