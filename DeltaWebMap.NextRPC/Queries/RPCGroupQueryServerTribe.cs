using LibDeltaSystem.WebFramework.WebSockets.Groups;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Queries
{
    public class RPCGroupQueryServerTribe : RPCGroupQueryServer
    {
        public int tribe_id;
        public bool any_tribe_id;

        public override bool CheckIfAuthorized(WebSocketGroupQuery request)
        {
            //Make sure the type of group matches
            if (request.GetType() != typeof(RPCGroupQueryServerTribe))
                return false;

            //Check if server ID matches
            RPCGroupQueryServerTribe query = (RPCGroupQueryServerTribe)request;
            if (query.server_id != server_id)
                return false;

            //Check if the the tribe ID matches (or if any is allowed)
            if (!any_tribe_id && query.tribe_id != tribe_id)
                return false;

            return true;
        }
    }
}
