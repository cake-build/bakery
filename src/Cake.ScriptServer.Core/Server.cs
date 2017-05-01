using System;
using Cake.ScriptServer.Core.Models;
using Cake.ScriptServer.Core.RequesHandlers;

namespace Cake.ScriptServer.Core
{
    public sealed class Server : IServer
    {
        private readonly IAliasRequestHandler _aliasRequestHandler;
        private readonly IResponseWriter _responseWriter;
        private readonly IDataContractSerializer _serializer;

        public Server(
            IAliasRequestHandler aliasRequestHandler,
            IResponseWriter responseWriter,
            IDataContractSerializer serializer = null)
        {
            _aliasRequestHandler = aliasRequestHandler ?? throw new ArgumentNullException(nameof(aliasRequestHandler));
            _responseWriter = responseWriter ?? throw new ArgumentNullException(nameof(responseWriter));

            // Default
            _serializer = serializer ?? new DataContractSerializer();
        }

        public void Handle(string request)
        {
            var requestModel = _serializer.Deserialize<Request>(request);

            switch (requestModel.Method)
            {
                case Method.GenerateAlias:
                    HandleGenerateAlias(_serializer.Deserialize<Request<GenerateAliasRequest>>(request));
                    break;
                case Method.SetFile:
                    break;
                case Method.UpdateBuffer:
                    break;
            }
        }

        private void HandleGenerateAlias(Request<GenerateAliasRequest> request)
        {
            if (request?.Payload == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = _aliasRequestHandler.Handle(request.Payload);

            _responseWriter.WriteResponse(_serializer.Serialize(response));
        }
    }
}
