﻿using LibDeltaSystem.WebFramework.WebSockets.Groups;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Queries
{
    public class RPCGroupQueryServer : IRPCGroupQuery
    {
        public ObjectId server_id;

        public override bool CheckIfAuthorized(WebSocketGroupQuery request)
        {
            //Make sure the type of group matches
            if (request.GetType() != typeof(RPCGroupQueryServer))
                return false;

            //Check if server ID matches
            RPCGroupQueryServer query = (RPCGroupQueryServer)request;
            if (query.server_id != server_id)
                return false;

            return true;
        }
    }
}
