using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC.Entities.Comms
{
    /// <summary>
    /// Outgoing RPC message to clients, in it's final form
    /// </summary>
    public class RPCOutgoing
    {
        public string command;
        public object payload;

        public const string COMMAND_NAME_RPC = "RPC_MESSAGE";
        public const string COMMAND_NAME_LOGIN = "LOGIN_COMPLETED";
        public const string COMMAND_NAME_GROUPS = "GROUPS_UPDATED";
    }
}
