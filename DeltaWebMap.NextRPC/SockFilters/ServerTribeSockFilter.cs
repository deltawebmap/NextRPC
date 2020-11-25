using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.SockFilters
{
    public class ServerTribeSockFilter : ISockFilter
    {
        public ServerTribeSockFilter(ObjectId server, int tribeId)
        {
            this.server = server;
            this.tribeId = tribeId;
        }

        private ObjectId server;
        private int tribeId;

        public bool CheckFilter(RPCConnection connection)
        {
            if(connection.currentServers.TryGetValue(server, out int? serverTribe))
            {
                if (!serverTribe.HasValue)
                    return true; //Is admin
                return serverTribe.Value == tribeId;
            }
            return false;
        }
    }
}
