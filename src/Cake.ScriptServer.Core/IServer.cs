namespace Cake.ScriptServer.Core
{
    public interface IServer
    {
        void Handle(string request);
    }
}
