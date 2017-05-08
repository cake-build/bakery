using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public enum StatusCode
    {
        [EnumMember]
        Error = -1,

        [EnumMember]
        Ok = 0,
    }
}
