using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.ScriptServer.Reflection
{
    public static class CSharpAliasProvider
    {
        private static readonly Dictionary<string, string> Lookup = new Dictionary<string, string>
        {
            { "T:System.Boolean", "bool" },
            { "T:System.Byte", "byte" },
            { "T:System.SByte", "sbyte" },
            { "T:System.Char", "char" },
            { "T:System.Decimal", "decimal" },
            { "T:System.Double", "double" },
            { "T:System.Single", "float" },
            { "T:System.Int32", "int" },
            { "T:System.UInt32", "uint" },
            { "T:System.Int64", "long" },
            { "T:System.UInt64", "ulong" },
            { "T:System.Object", "object" },
            { "T:System.Int16", "short" },
            { "T:System.UInt16", "ushort" },
            { "T:System.String", "string" },
            { "T:System.Void", "void" }
        };

        public static string GetTypeAlias(TypeSignature type)
        {
            return Lookup.ContainsKey(type.CRef) 
                ? Lookup[type.CRef] : null;
        }
    }
}
