using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public class GenerateAliasRequest
    {
        [DataMember(Name = "assemblyPath", IsRequired = true)]
        public string AssemblyPath { get; set; }

        [DataMember(Name = "verifyAssembly", IsRequired = false, EmitDefaultValue = true)]
        public bool VerifyAssembly { get; set; }
    }
}