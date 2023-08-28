using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace ModelBuilder;

public class ModelBuilderService : IModelBuilderService
{

    private readonly string[] m_excludedColumns = {"OptimisticLockField", "GCRecord"};
    private readonly string[] m_excludedTables = {"XPObjectType", "XPWeakReference", "XPWeakReferenceType", "XPWeakReferenceValue"};
    
    public DbModel Model { get; set; } = new DbModel();

    public ConnState ConnectionState { get; set; } = ConnState.NotInitialized;

    public DbModel GenerateModel(string p_tableName)
    {
        var excludedColumns = Model.ExcludedColumns.Split(',').Concat(m_excludedColumns).ToArray();

        var modelString = new StringBuilder();
        using (var connection = new SQLiteConnection(ConnString()))
        {
            connection.Open();
            using (var command = new SQLiteCommand($"PRAGMA table_info({p_tableName})", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    modelString.AppendLine($"public class {Model.Name}");
                    modelString.AppendLine("{");
                    while (reader.Read())
                    {
                        var columnName = reader["name"].ToString()!;
                        var dataType = reader["type"].ToString()!;
                        var dValue = reader["dflt_value"].ToString()!; 
                        if(excludedColumns.Contains(columnName))
                            continue;
                        modelString.AppendLine(CreateLine(dataType, columnName, dValue));
                    }
                    modelString.AppendLine("}");
                }
            }
        }

        Model.ModelDefinition = modelString.ToString();
        return Model;

    }

    private string CreateLine(string p_dataType, string p_columnName, string p_defaultValue)
    {
        switch (p_dataType.ToUpper())
        {
            case "INTEGER": case "INT":
                return $"    public int {p_columnName} {{ get; set; }}" + ((!string.IsNullOrEmpty(p_defaultValue)) ? $"        = {p_defaultValue};" : "");
            case "TEXT":
                return $"    public string {p_columnName} {{ get; set; }}" + ((!string.IsNullOrEmpty(p_defaultValue)) ? $"        = \"{p_defaultValue}\";" : "");
            case "REAL":
                return $"    public double {p_columnName} {{ get; set; }}" + ((!string.IsNullOrEmpty(p_defaultValue)) ? $"        = {p_defaultValue};" : "");
            case "BLOB":
                return $"    public byte[] {p_columnName} {{ get; set; }}";
            case "BIT":
                return $"    public bool {p_columnName} {{ get; set; }}" + ((!string.IsNullOrEmpty(p_defaultValue)) ? $"        = {p_defaultValue};" : "");
            default:
                if (p_dataType.ToUpper().Contains("VARCHAR"))
                {
                    return $"    public string {p_columnName} {{ get; set; }}" + ((!string.IsNullOrEmpty(p_defaultValue)) ? $"        = \"{p_defaultValue}\";" : "");
                }
                else
                {
                    return $"    public {p_dataType} {p_columnName} {{ get; set; }}";
                }
                break;
        }
    }
    private string ConnString()
    {
        if (Model.DatabasePath == string.Empty)
            throw new Exception("DatabasePath is empty");
        return ConnString(Model.DatabasePath);
    }
    private string ConnString(string p_databasePath)
    {
        if (string.IsNullOrEmpty(p_databasePath))
            throw new Exception("DatabasePath is empty");
        if (!File.Exists(p_databasePath))
            throw new Exception("DatabasePath does not exist");
        return $"Data Source={p_databasePath};Version=3;";
    }
    public string[] GetTables()
    {
        var excludedTables = Model.ExcludedTables.Split(',').Concat(m_excludedTables).ToArray();
        using var connection = new SQLiteConnection(ConnString());
        connection.Open();

        var schema = connection.GetSchema("Tables");

        var dt = new string[schema.Rows.Count];
        foreach (DataRow row in schema.Rows)
        {           
            var tableName = row["TABLE_NAME"].ToString();
            if(excludedTables.Contains(tableName))
                continue;
            if(string.IsNullOrEmpty(tableName))
                continue;
            dt[Array.IndexOf(dt, null)] = tableName;
        }

        return dt;
    }

    public bool CheckConnection()
    {
        using var connection = new SQLiteConnection(ConnString(Model.DatabasePath));
        try
        {
            connection.Open();
            GetTables();
            ConnectionState = ConnState.Connected;
            return true;
        }
        catch (Exception ex)
        {
            ConnectionState = ConnState.Failed;
            return false;
        }
    }
}