﻿using LibDeltaSystem.WebFramework.WebSockets.Groups;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Queries
{
    public class RPCGroupQueryUser : IRPCGroupQuery
    {
        public ObjectId user_id;

        public override bool CheckIfAuthorized(WebSocketGroupQuery request)
        {
            //Make sure the type of group matches
            if (request.GetType() != typeof(RPCGroupQueryUser))
                return false;

            //Check if user ID matches
            RPCGroupQueryUser query = (RPCGroupQueryUser)request;
            if (query.user_id != user_id)
                return false;

            return true;
        }
    }
}
