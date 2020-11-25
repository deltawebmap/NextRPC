using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.SockFilters
{
    public class ServerSockFilter : ISockFilter
    {
        public ServerSockFilter(ObjectId server)
        {
            this.server = server;
        }

        private ObjectId server;

        public bool CheckFilter(RPCConnection connection)
        {
            return connection.currentServers.ContainsKey(server);
        }
    }
}
