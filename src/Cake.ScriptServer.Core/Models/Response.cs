using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public class Response<TPayload>
    {
        public StatusCode Status { get; set; }

        public TPayload Payload { get; set; }
    }
}
