using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Cake.ScriptServer.Core.Models
{
    [DataContract]
    public class ScriptResponse
    {
        private HashSet<string> _references;
        private HashSet<string> _usings;

        [IgnoreDataMember]
        public static ScriptResponse Empty = new ScriptResponse();

        [DataMember(Name = "source", IsRequired = true)]
        public string Source { get; set; }

        [DataMember(Name = "references", IsRequired = false)]
        public HashSet<string> References => _references ?? (_references = new HashSet<string>());

        [DataMember(Name = "usings", IsRequired = false)]
        public HashSet<string> Usings => _usings ?? (_usings = new HashSet<string>());
    }
}
