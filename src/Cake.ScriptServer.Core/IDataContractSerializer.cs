namespace Cake.ScriptServer.Core
{
    public interface IDataContractSerializer
    {
        string Serialize<T>(T obj);

        T Deserialize<T>(string content) where T : class;
    }
}
