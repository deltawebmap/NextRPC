using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Entities
{
    public class RPCReceivePayload
    {
        public string command;
        public Dictionary<string, string> payload;
    }
}
