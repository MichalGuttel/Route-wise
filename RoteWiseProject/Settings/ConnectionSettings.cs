﻿using Neo4j.Driver;

namespace RoteWiseProject.Settings
{
    public class ConnectionSettings : IConnectionSettings
    {
        public string Uri { get; private set; }

        public IAuthToken AuthToken { get; private set; }

        public ConnectionSettings(string uri, IAuthToken authToken)
        {
            Uri = uri;
            AuthToken = authToken;
        }

        public static ConnectionSettings CreateBasicAuth(string uri, string username, string password)
        {
            return new ConnectionSettings(uri, AuthTokens.Basic(username, password));
        }
    }
}
