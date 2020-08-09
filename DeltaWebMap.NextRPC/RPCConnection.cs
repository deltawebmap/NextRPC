using DeltaWebMap.NextRPC.Entities;
using DeltaWebMap.NextRPC.Entities.Comms;
using DeltaWebMap.NextRPC.Queries;
using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework.WebSockets.Groups;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC
{
    public class RPCConnection : GroupWebSocketService
    {
        /// <summary>
        /// The user authenticated
        /// </summary>
        public DbUser user;

        /// <summary>
        /// The token used to issue this request
        /// </summary>
        public DbToken token;

        public RPCConnection(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task<List<WebSocketGroupQuery>> AuthenticateGroupsQuery()
        {
            //If we're not logged in, return no groups
            if(user == null)
                return new List<WebSocketGroupQuery>();

            //Create queries
            var queries = new List<WebSocketGroupQuery>();

            //Add user query
            queries.Add(new RPCGroupQueryUser
            {
                user_id = user._id
            });

            //Add servers we're in
            var playerServers = await user.GetGameServersAsync(conn);
            var adminServers = await user.GetAdminedServersAsync(conn);
            List<ObjectId> serverIds = new List<ObjectId>();
            foreach (var s in playerServers)
            {
                queries.Add(new RPCGroupQueryServer
                {
                    server_id = s.Item1._id
                });
                queries.Add(new RPCGroupQueryServerTribe
                {
                    server_id = s.Item1._id,
                    tribe_id = s.Item2.tribe_id,
                    any_tribe_id = s.Item1.CheckIsUserAdmin(user)
                });
                serverIds.Add(s.Item1._id);
            }
            foreach (var s in adminServers)
            {
                queries.Add(new RPCGroupQueryServerAdmin
                {
                    server_id = s._id
                });
                if (serverIds.Contains(s._id))
                    continue;
                queries.Add(new RPCGroupQueryServer
                {
                    server_id = s._id
                });
            }

            return queries;
        }

        public override WebSocketGroupHolder GetGroupHolder()
        {
            return Program.holder;
        }

        public override async Task<bool> OnPreRequest()
        {
            return true;
        }

        public override Task OnReceiveBinary(byte[] data, int length)
        {
            throw new Exception("Binary data is not supported.");
        }

        public override async Task OnReceiveText(string data)
        {
            //Decode
            RPCReceivePayload request = JsonConvert.DeserializeObject<RPCReceivePayload>(data);

            //Handle commands
            switch(request.command)
            {
                case "LOGIN": await HandleCommandAuth(request.payload); break;
            }
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        private async Task HandleCommandAuth(Dictionary<string, string> payload)
        {
            //Get the acccess token
            if (!payload.ContainsKey("ACCESS_TOKEN"))
                return;
            string tokenString = payload["ACCESS_TOKEN"];

            //Make sure we're not already authenticated
            if (user != null)
                return;

            //Authenticate this token
            token = await conn.GetTokenByTokenAsync(tokenString);
            if (token == null)
            {
                //Failed!
                await SendOutgoingCommand(RPCOutgoing.COMMAND_NAME_LOGIN, new RPCLoginCompletedPayload
                {
                    success = false
                });
                return;
            }

            //Get user
            user = await conn.GetUserByIdAsync(token.user_id);
            if (user == null)
                return;

            //Log
            conn.Log("RPCConnection-HandleCommandAuth", $"[SESSION {_request_id}] Logged in user {user._id.ToString()}", DeltaLogLevel.Debug);

            //Send success message
            await SendOutgoingCommand(RPCOutgoing.COMMAND_NAME_LOGIN, new RPCLoginCompletedPayload
            {
                success = true,
                user = new RPCLoginCompletedPayload.RPCLoginCompletedPayload_User
                {
                    name = user.screen_name,
                    id = user.id,
                    icon = user.profile_image_url,
                    steam_id = user.steam_id
                }
            });

            //Refresh
            await RefreshGroups();
        }

        private async Task SendOutgoingCommand(string command, object payload)
        {
            //Create payload
            var p = new RPCOutgoing
            {
                command = command,
                payload = payload
            };

            //Send
            await SendData(JsonConvert.SerializeObject(p));
        }

        public override async Task OnGroupsUpdated()
        {
            //Create list of groups
            List<string> names = new List<string>();
            foreach(var n in groups)
            {
                names.Add(n.identifier.GetType().Name);
            }

            //Send
            await SendOutgoingCommand(RPCOutgoing.COMMAND_NAME_GROUPS, new RPCGroupChangedPayload
            {
                groups = names
            });
        }
    }
}
