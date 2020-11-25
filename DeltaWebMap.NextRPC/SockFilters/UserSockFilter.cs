using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.SockFilters
{
    public class UserSockFilter : ISockFilter
    {
        public UserSockFilter(ObjectId user)
        {
            this.user = user;
        }

        private ObjectId user;
        
        public bool CheckFilter(RPCConnection connection)
        {
            return connection.currentUserId == user;
        }
    }
}
