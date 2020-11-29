using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework;
using LibDeltaSystem.WebFramework.WebSockets.OpcodeSock;
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
    public class RPCConnection : DeltaOpcodeUserWebSocketService
    {
        public RPCConnection(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
            currentServers = new ConcurrentDictionary<ObjectId, int?>();
        }

        public ObjectId? currentUserId;
        public ConcurrentDictionary<ObjectId, int?> currentServers; //A null key indicates that this user has admin access. If it is non-null, that is their tribe ID

        public override async Task OnSockOpened()
        {
            lock (Program.connections)
                Program.connections.Add(this);
        }

        public override async Task OnSockClosed()
        {
            lock (Program.connections)
                Program.connections.Remove(this);
        }

        public override async Task OnUserLoginSuccess()
        {
            await RefreshGroups();
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
            await SendMessage(OUT_OPCODE_GROUPREFRESH, msg);
        }

        public const string OUT_OPCODE_RPCMSG = "RPC_MESSAGE";
        public const string OUT_OPCODE_GROUPREFRESH = "GROUP_REFRESH";
    }
}
