namespace ModelBuilder;

public interface IModelBuilderService
{
    public DbModel Model { get; set; }
    public DbModel GenerateModel(string p_tableName);
    public string[] GetTables();
    public bool CheckConnection();
}