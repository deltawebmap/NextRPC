using LibDeltaSystem.WebFramework.WebSockets.Groups;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Queries
{
    public class IRPCGroupQuery : WebSocketGroupQuery
    {
        public override bool CheckIfAuthorized(WebSocketGroupQuery request)
        {
            //Make sure the type of group matches
            if (request.GetType() != typeof(IRPCGroupQuery))
                return false;

            return true;
        }
    }
}
