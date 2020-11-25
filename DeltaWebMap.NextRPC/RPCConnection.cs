using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC
{
    public class RPCConnection : DeltaWebService
    {
        public RPCConnection(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
            incomingBuffer = new byte[1024];
            channel = Channel.CreateUnbounded<ISockCommand>();
            currentServers = new ConcurrentDictionary<ObjectId, int?>();
        }

        private WebSocket sock;
        private byte[] incomingBuffer;
        private Channel<ISockCommand> channel;

        private DbUser user;
        private DbToken token;

        public ObjectId? currentUserId;
        public ConcurrentDictionary<ObjectId, int?> currentServers; //A null key indicates that this user has admin access. If it is non-null, that is their tribe ID

        public override async Task OnRequest()
        {
            //Accept WebSocket
            if (!e.WebSockets.IsWebSocketRequest)
            {
                await WriteString("Expected WebSocket request to this endpoint.", "text/plain", 400);
                return;
            }
            sock = await e.WebSockets.AcceptWebSocketAsync();

            //Add to list
            lock (Program.connections)
                Program.connections.Add(this);

            //Begin get
            CancellationToken cancellationToken = new CancellationToken();
            Task<WebSocketReceiveResult> receiveTask = sock.ReceiveAsync(new ArraySegment<byte>(incomingBuffer, 0, incomingBuffer.Length), cancellationToken);
            Task<ISockCommand> commandsTask = channel.Reader.ReadAsync(cancellationToken).AsTask();

            //Loop
            string incomingTextBuffer = "";
            while (true)
            {
                //Wait for something to complete
                await Task.WhenAny(receiveTask, commandsTask);

                //Handle receiveTask
                if (receiveTask.IsCompleted)
                {
                    WebSocketReceiveResult result = receiveTask.Result;
                    if (result.CloseStatus.HasValue)
                        break;
                    if (result.MessageType != WebSocketMessageType.Text)
                        break;

                    //Write to buffer
                    incomingTextBuffer += Encoding.UTF8.GetString(incomingBuffer, 0, result.Count);

                    //Check if this is the end
                    if (result.EndOfMessage)
                    {
                        await OnSockCommandReceive(incomingTextBuffer);
                        incomingTextBuffer = "";
                    }

                    //Get next
                    receiveTask = sock.ReceiveAsync(new ArraySegment<byte>(incomingBuffer, 0, incomingBuffer.Length), cancellationToken);
                }

                //Handle commandsTask
                if (commandsTask.IsCompleted)
                {
                    await OnInternalCommandReceive(commandsTask.Result);
                    commandsTask = channel.Reader.ReadAsync(cancellationToken).AsTask();
                }
            }

            //Remove from list
            lock (Program.connections)
                Program.connections.Remove(this);
        }

        public void EnqueueMessage(ISockCommand cmd)
        {
            channel.Writer.WriteAsync(cmd);
        }

        private async Task OnSockCommandReceive(string cmd)
        {
            //Decode
            JObject data = JsonConvert.DeserializeObject<JObject>(cmd);
            string opcode = (string)data["opcode"];
            JObject payload = (JObject)data["payload"];

            //Handle
            switch(opcode)
            {
                case IN_OPCODE_LOGIN: await OnLoginRequest(payload); break;
            }
        }

        private async Task OnInternalCommandReceive(ISockCommand cmd)
        {
            await cmd.HandleCommand(this);
        }

        public async Task RefreshGroups()
        {
            //Set user
            currentUserId = user._id;

            //Clear current servers
            currentServers.Clear();

            //Look up the servers the user has a player profile in and add them
            var playerServers = await user.GetGameServersAsync(conn);
            foreach (var p in playerServers)
                currentServers.TryAdd(p.Item1._id, p.Item2.tribe_id);

            //Look up the servers the user is admin in and add them
            var adminServers = await user.GetAdminedServersAsync(conn);
            foreach (var p in playerServers)
                currentServers.AddOrUpdate(p.Item1._id, (key) => null, (key, oldValue) => null);

            //Tell the user that their groups have refreshed
            JObject msg = new JObject();
            msg["user_id"] = user.id;
            msg["server_count"] = currentServers.Count;
            await SendMessage(OUT_OPCODE_LOGINSTATE, msg);
        }

        private async Task OnLoginRequest(JObject data)
        {
            //Get token
            token = await conn.GetTokenByTokenAsync((string)data["access_token"]);
            if(token == null)
            {
                await SendLoginStatus(false, "Token Invalid");
                return;
            }

            //Get user
            user = await conn.GetUserByIdAsync(token.user_id);
            if (user == null)
            {
                await SendLoginStatus(false, "User Invalid (bad!)");
                return;
            }

            //Issue OK
            await SendLoginStatus(true, "OK; Logged in user " + user.id);

            //Update groups
            await RefreshGroups();
        }

        private async Task SendLoginStatus(bool success, string message)
        {
            JObject msg = new JObject();
            msg["success"] = success;
            msg["message"] = message;
            await SendMessage(OUT_OPCODE_LOGINSTATE, msg);
        }

        public const string IN_OPCODE_LOGIN = "LOGIN";
        public const string OUT_OPCODE_RPCMSG = "RPC_MESSAGE";
        public const string OUT_OPCODE_GROUPREFRESH = "GROUP_REFRESH";
        public const string OUT_OPCODE_LOGINSTATE = "LOGIN_STATUS";

        public async Task SendMessage(string opcode, JObject payload)
        {
            //Create
            JObject p = new JObject();
            p["opcode"] = opcode;
            p["payload"] = payload;

            //Serialize
            string data = JsonConvert.SerializeObject(p);
            byte[] dataPayload = Encoding.UTF8.GetBytes(data);

            //Send
            await sock.SendAsync(new ArraySegment<byte>(dataPayload, 0, dataPayload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
