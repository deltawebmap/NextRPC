﻿using LibDeltaSystem.WebFramework.WebSockets.Groups;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Queries
{
    public class RPCGroupQueryServerAdmin : IRPCGroupQuery
    {
        public ObjectId server_id;

        public override bool CheckIfAuthorized(WebSocketGroupQuery request)
        {
            //Make sure the type of group matches
            if (request.GetType() != typeof(RPCGroupQueryServerAdmin))
                return false;

            //Check if server ID matches
            RPCGroupQueryServerAdmin query = (RPCGroupQueryServerAdmin)request;
            if (query.server_id != server_id)
                return false;

            //Return if admin
            return true;
        }
    }
}
