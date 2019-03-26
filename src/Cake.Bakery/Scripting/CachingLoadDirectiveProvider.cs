using System.Collections.Generic;
using System.Linq;
using Cake.Core.IO;
using Cake.Core.Scripting.Analysis;
using Cake.Core.Scripting.Processors.Loading;

namespace Cake.Bakery.Scripting
{
    internal sealed class CachingLoadDirectiveProvider : ILoadDirectiveProvider
    {
        private readonly IDictionary<LoadReference, IReadOnlyList<FilePath>> _cache;
        private readonly IReadOnlyList<ILoadDirectiveProvider> _loadDirectiveProviders;

        public CachingLoadDirectiveProvider(IEnumerable<ILoadDirectiveProvider> loadDirectiveProviders)
        {
            _loadDirectiveProviders = loadDirectiveProviders?.ToList() ?? throw new System.ArgumentNullException(nameof(loadDirectiveProviders));
            _cache = new Dictionary<LoadReference, IReadOnlyList<FilePath>>(new LoadReferenceComparer());
        }

        public bool CanLoad(IScriptAnalyzerContext context, LoadReference reference)
        {
            return _loadDirectiveProviders.Any(x => x.CanLoad(context, reference));
        }

        public void Load(IScriptAnalyzerContext context, LoadReference reference)
        {
            if (!_cache.TryGetValue(reference, out IReadOnlyList<FilePath> result))
            {
                var provider = _loadDirectiveProviders.FirstOrDefault(x => x.CanLoad(context, reference));
                if (provider == null)
                {
                    return;
                }

                var interceptor = new InterceptingScriptAnalyzerContext();
                provider.Load(interceptor, reference);
                result = interceptor.FilePaths;

                _cache.Add(reference, result);
            }

            foreach (var file in result ?? Enumerable.Empty<FilePath>())
            {
                context.Analyze(file);
            }
        }

        private class InterceptingScriptAnalyzerContext : IScriptAnalyzerContext
        {
            public List<FilePath> FilePaths { get; } = new List<FilePath>();

            public FilePath Root => throw new System.NotImplementedException();

            public IScriptInformation Current => throw new System.NotImplementedException();

            public void AddScriptError(string error)
            {
                throw new System.NotImplementedException();
            }

            public void AddScriptLine(string line)
            {
                throw new System.NotImplementedException();
            }

            public void Analyze(FilePath scriptPath)
            {
                FilePaths.Add(scriptPath);
            }
        }
    }
}