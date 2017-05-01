using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public enum Method : uint
    {
        [EnumMember]
        GenerateAlias = 0,

        [EnumMember]
        SetFile = 1,

        [EnumMember]
        UpdateBuffer = 2,
    }
}
