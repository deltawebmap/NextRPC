using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Entities.Comms
{
    public class RPCLoginCompletedPayload
    {
        public bool success;
        public RPCLoginCompletedPayload_User user;

        public class RPCLoginCompletedPayload_User
        {
            public string name;
            public string id;
            public string steam_id;
            public string icon;
        }
    }
}
