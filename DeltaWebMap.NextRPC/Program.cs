using DeltaWebMap.NextRPC.Entities;
using DeltaWebMap.NextRPC.Entities.Comms;
using LibDeltaSystem;
using LibDeltaSystem.WebFramework;
using LibDeltaSystem.WebFramework.WebSockets.Entities;
using LibDeltaSystem.WebFramework.WebSockets.Groups;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC
{
    class Program
    {
        public static DeltaConnection conn;
        public static WebSocketGroupHolder holder;
        public static ConcurrentQueue<RPCInternalMessage> queue;
        private static Thread processingThread;

        public const byte APP_VERSION_MAJOR = 0;
        public const byte APP_VERSION_MINOR = 5;

        static void Main(string[] args)
        {
            //Create
            holder = new WebSocketGroupHolder();
            queue = new ConcurrentQueue<RPCInternalMessage>();

            //Create processing thread
            processingThread = new Thread(() =>
            {
                while(true)
                {
                    if (queue.TryDequeue(out RPCInternalMessage m))
                        HandleMessage(m);
                    else
                        Thread.Sleep(10);
                }
            });
            processingThread.IsBackground = true;
            processingThread.Start();

            //Connect to database
            conn = DeltaConnection.InitDeltaManagedApp(args, APP_VERSION_MAJOR, APP_VERSION_MINOR, new RpcNetwork());

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

        private static void HandleMessage(RPCInternalMessage m)
        {
            try
            {
                //Find the group to send to
                var group = holder.FindGroup(m.query);
                if (group == null)
                    return;

                //Get the external message to send
                var msg = new RPCOutgoing
                {
                    command = RPCOutgoing.COMMAND_NAME_RPC,
                    payload = m.GetExternalMessage()
                };
                byte[] msgBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));

                //Log
                conn.Log("NextRPC-HandleMessage", $"Dispatching {msgBytes.Length} byte payload with opcode {m.opcode} to {group.clients.Count} clients. Query type {m.query.GetType().Name}.", DeltaLogLevel.Debug);

                //Send
                group.SendDistributedMessage(new PackedWebSocketMessage
                {
                    data = msgBytes,
                    length = msgBytes.Length,
                    type = System.Net.WebSockets.WebSocketMessageType.Text
                }, new List<GroupWebSocketService>());
            } catch(Exception ex)
            {
                conn.Log("NextRPC-HandleMessage", "Hit exception attempting to handle RPC message.", DeltaLogLevel.High);
            }
        }
    }
}
