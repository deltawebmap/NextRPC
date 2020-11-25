using DeltaWebMap.NextRPC.SockCommands;
using DeltaWebMap.NextRPC.SockFilters;
using LibDeltaSystem;
using LibDeltaSystem.CoreNet;
using LibDeltaSystem.CoreNet.IO;
using LibDeltaSystem.RPC;
using LibDeltaSystem.Tools;
using LibDeltaSystem.WebFramework;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC
{
    class Program
    {
        public static DeltaConnection conn;
        public static List<RPCConnection> connections = new List<RPCConnection>();

        public const byte APP_VERSION_MAJOR = 1;
        public const byte APP_VERSION_MINOR = 2;

        static void Main(string[] args)
        {
            //Connect to database
            conn = DeltaConnection.InitDeltaManagedApp(args, DeltaCoreNetServerType.API_RPC, APP_VERSION_MAJOR, APP_VERSION_MINOR);
            conn.net.BindReceiveEvent(RouterConnection.OPCODE_SYS_RPC, OnIncomingRPCCommand);

            //Launch server
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            //Start server
            DeltaWebServer server = new DeltaWebServer(conn, conn.GetUserPort(0));
            server.AddService(new RPCConnectionDefinition());
            await server.RunAsync();
        }

        private static void OnIncomingRPCCommand(RouterMessage msg)
        {
            //Decode header
            byte cmdOpcode = msg.payload[0];
            byte cmdFlags = msg.payload[1];
            
            //Switch on message opcode
            switch(cmdOpcode)
            {
                case RPCMessageTool.TYPECODE_MESSAGE: OnIncomingRPCCommand_RPCMessage(msg.payload); break;
                case RPCMessageTool.TYPECODE_GROUP_RESET: OnIncomingRPCCommand_RefreshGroups(msg.payload); break;
                case RPCMessageTool.TYPECODE_PRIVILEGED_MESSAGE_SERVER: OnIncomingRPCCommand_PrivledgedMessage(msg.payload); break;
                default: throw new Exception("Unknwon command opcode " + cmdOpcode + "!");
            }
        }

        private static void OnIncomingRPCCommand_RPCMessage(byte[] payload)
        {
            //Read header
            RPCOpcode opcode = (RPCOpcode)BitConverter.ToInt32(payload, 2);
            byte filterType = payload[6];
            byte reserved = payload[7];
            ushort filterSize = BitConverter.ToUInt16(payload, 8);
            uint payloadSize = BitConverter.ToUInt32(payload, 10 + filterSize);

            //Read filter
            ISockFilter filter = DecodeCommandFilter(payload, filterType, (byte)filterSize, 10, out ObjectId? serverId);

            //Read payload
            JObject data = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(payload, 10 + filterSize + 4, (int)payloadSize));

            //Handle
            DispatchMessage(filter, new SendRPCMessageCommand(opcode.ToString(), serverId, data));
        }

        private static void OnIncomingRPCCommand_RefreshGroups(byte[] payload)
        {
            //Read header
            byte filterType = payload[2];
            byte reserved = payload[3];
            ushort filterSize = BitConverter.ToUInt16(payload, 4);

            //Read filter
            ISockFilter filter = DecodeCommandFilter(payload, filterType, (byte)filterSize, 6, out ObjectId? serverId);

            //Handle
            DispatchMessage(filter, new RefreshGroupsRequestCommand());
        }

        private static void OnIncomingRPCCommand_PrivledgedMessage(byte[] payload)
        {
            //Read header
            RPCOpcode opcode = (RPCOpcode)BitConverter.ToInt32(payload, 2);
            ObjectId serverId = BinaryTool.ReadMongoID(payload, 6);
            int payloadCount = BitConverter.ToInt32(payload, 18);

            //Clone list
            List<RPCConnection> clientsSafe = new List<RPCConnection>();
            lock (connections)
                clientsSafe.AddRange(connections);

            //Begin reading array
            int offset = 22;
            for(int i = 0; i<payloadCount; i++)
            {
                //Read header data
                int tribeId = BitConverter.ToInt32(payload, offset + 0);
                int payloadLength = BitConverter.ToInt32(payload, offset + 4);
                offset += 8;

                //Decode payload
                JObject data = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(payload, offset, payloadLength));

                //Create filter and command
                ISockFilter filter = new ServerTribeSockFilter(serverId, tribeId);
                var cmd = new SendRPCMessageCommand(opcode.ToString(), serverId, data);

                //Search to send messages
                foreach (var c in clientsSafe)
                {
                    //Filter
                    if (!filter.CheckFilter(c))
                        continue;

                    //Send
                    c.EnqueueMessage(cmd);
                }
            }
        }

        private static ISockFilter DecodeCommandFilter(byte[] payload, byte type, byte length, int offset, out ObjectId? serverId)
        {
            if(type == 0)
            {
                //User ID, not targetting a server
                ObjectId user = BinaryTool.ReadMongoID(payload, offset);
                serverId = null;
                return new UserSockFilter(user);
            } else if(type == 1)
            {
                //User ID, targetting server
                ObjectId user = BinaryTool.ReadMongoID(payload, offset);
                serverId = BinaryTool.ReadMongoID(payload, offset + 12);
                return new UserSockFilter(user);
            } else if (type == 2)
            {
                //Server ID, all tribes
                serverId = BinaryTool.ReadMongoID(payload, offset);
                return new ServerSockFilter(serverId.Value);
            } else if (type == 3)
            {
                //Server ID, specific tribe
                serverId = BinaryTool.ReadMongoID(payload, offset);
                int tribeId = BinaryTool.ReadInt32(payload, offset + 12);
                return new ServerTribeSockFilter(serverId.Value, tribeId);
            } else
            {
                throw new Exception("Unknown filter type " + type + "!");
            }
        }

        private static void DispatchMessage(ISockFilter filter, ISockCommand cmd)
        {
            //Clone list
            List<RPCConnection> clientsSafe = new List<RPCConnection>();
            lock (connections)
                clientsSafe.AddRange(connections);

            //Search
            foreach(var c in clientsSafe)
            {
                //Filter
                if (!filter.CheckFilter(c))
                    continue;

                //Send
                c.EnqueueMessage(cmd);
            }
        }
    }
}
