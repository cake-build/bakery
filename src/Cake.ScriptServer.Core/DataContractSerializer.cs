using System.IO;
using System.Runtime.Serialization.Json;

namespace Cake.ScriptServer.Core
{
    public sealed class DataContractSerializer : IDataContractSerializer
    {
        public string Serialize<T>(T obj)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            {
                serializer.WriteObject(stream, obj);
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        public T Deserialize<T>(string content) where T : class
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(content);
                writer.Flush();
                stream.Position = 0;

                return serializer.ReadObject(stream) as T;
            }
        }
    }
}
