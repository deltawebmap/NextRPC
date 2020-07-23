using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Entities
{
    public class RPCExternalMessage
    {
        public object payload;
        public int opcode;
        public string source;
        public string target_server;
    }
}
