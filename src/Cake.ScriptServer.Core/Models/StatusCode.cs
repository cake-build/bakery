using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public enum StatusCode : int
    {
        [EnumMember]
        Error = -1,

        [EnumMember]
        Ok = 0,
    }
}
