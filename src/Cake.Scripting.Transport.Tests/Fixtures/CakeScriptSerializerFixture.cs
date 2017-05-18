using System;
using System.IO;
using System.Reflection;
using System.Text;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Transport.Tests.Fixtures
{
    public class CakeScriptSerializerFixture : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly Assembly _assembly;
        private readonly string _resourcePath;

        public CakeScriptSerializerFixture()
        {
            _assembly = typeof(CakeScriptSerializerFixture).GetTypeInfo().Assembly;
            _resourcePath = "Cake.Scripting.Transport.Tests.Data";
            _stream = new MemoryStream();

            Writer = new BinaryWriter(_stream);
            Reader = new BinaryReader(_stream);
        }

        public BinaryWriter Writer { get; }

        public BinaryReader Reader { get; }

        public void ResetStreamPosition()
        {
            _stream.Position = 0;
        }

        public void ResetStream()
        {
            _stream.SetLength(0);
        }

        public CakeScript CreateCakeScriptFromResource(string name, int referencesLength, int usingsLength)
        {
            var resource = string.Concat($"{_resourcePath}.", name);
            using (var stream = _assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not load manifest resource stream.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return CreateCakeScript(reader.ReadToEnd(), referencesLength, usingsLength);
                }
            }
        }

        public CakeScript CreateCakeScript(string source, int referencesLength, int usingsLength)
        {
            var cakeScript = new CakeScript
            {
                Source = source
            };

            for (var i = 0; i < referencesLength; i++)
            {
                cakeScript.References.Add(Guid.NewGuid().ToString());
            }
            for (var i = 0; i < usingsLength; i++)
            {
                cakeScript.Usings.Add(Guid.NewGuid().ToString());
            }

            return cakeScript;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            Writer?.Dispose();
            Reader?.Dispose();
        }
    }
}
