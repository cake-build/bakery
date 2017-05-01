namespace Cake.ScriptServer.Core.RequesHandlers
{
    public interface IRequestHandler<in TRequestModel, out TResponseModel>
    {
        TResponseModel Handle(TRequestModel request);
    }
}
