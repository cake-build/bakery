using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Abstractions
{
    public interface IScriptGenerationService
    {
        CakeScript Generate(FileChange fileChange);
    }
}
