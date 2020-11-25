using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC.SockCommands
{
    public class SendRPCMessageCommand : ISockCommand
    {
        private JObject payload;
        private string opcode;
        private ObjectId? target_server;

        public SendRPCMessageCommand(string opcode, ObjectId? target_server, JObject payload)
        {
            this.opcode = opcode;
            this.target_server = target_server;
            this.payload = payload;
        }
        
        public async Task HandleCommand(RPCConnection conn)
        {
            //Create command
            JObject cmd = new JObject();
            cmd["opcode"] = opcode;
            cmd["target_server"] = target_server?.ToString();
            cmd["payload"] = payload;

            //Send
            await conn.SendMessage(RPCConnection.OUT_OPCODE_RPCMSG, cmd);
        }
    }
}
