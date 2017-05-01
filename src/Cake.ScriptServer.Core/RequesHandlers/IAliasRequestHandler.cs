using Cake.ScriptServer.Core.Models;

namespace Cake.ScriptServer.Core.RequesHandlers
{
    public interface IAliasRequestHandler : IRequestHandler<GenerateAliasRequest, ScriptResponse>
    {
    }
}
