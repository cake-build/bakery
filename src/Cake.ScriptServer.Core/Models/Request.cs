using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public class Request
    {
        [DataMember(Name = "method", IsRequired = true)]
        public Method Method { get; set; }
    }

    [DataContract]
    public class Request<TPayload> : Request
    {
        [DataMember(Name = "payload")]
        public TPayload Payload { get; set; }
    }
}
