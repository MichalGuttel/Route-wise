using Neo4j.Driver;

namespace RoteWiseProject.Settings
{
    public interface IConnectionSettings
    {
        string Uri { get; }

        IAuthToken AuthToken { get; }
    }
}
