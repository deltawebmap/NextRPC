using LibDeltaSystem.CoreHub.CoreNetwork;
using System;
using System.Collections.Generic;
using System.Text;
using LibDeltaSystem.Tools;
using DeltaWebMap.NextRPC.Queries;
using MongoDB.Bson;
using DeltaWebMap.NextRPC.Entities;
using LibDeltaSystem.WebFramework.WebSockets.Groups;
using LibDeltaSystem.CoreHub;

namespace DeltaWebMap.NextRPC
{
    public class RpcNetwork : BaseClientCoreNetwork
    {
        private const int FILTER_OFFSET = 2;

        public RpcNetwork()
        {
            SubscribeMessageOpcode(CoreNetworkOpcode.RPC_EVENT, OnMessageRPCMessageCommand);
            SubscribeMessageOpcode(CoreNetworkOpcode.RPC_REFRESH_GROUPS, OnMessageRPCRefreshGroups);
        }

        private byte[] OnMessageRPCRefreshGroups(CoreNetworkServer server, CoreNetworkOpcode op, byte[] payload)
        {
            //Read header
            byte filterType = payload[0];
            var query = DecodeFilter(filterType, payload, 1, out ObjectId? targetServerId);

            //Find a group using this query and read clients
            var group = Program.holder.FindGroup(query);
            if(group != null)
            {
                //Find clients
                GroupWebSocketService[] socks;
                lock(group.clients)
                {
                    socks = new GroupWebSocketService[group.clients.Count];
                    group.clients.CopyTo(socks);
                }

                //Refresh all
                foreach (var g in socks)
                    g.RefreshGroups();
            }

            return new byte[0];
        }

        private byte[] OnMessageRPCMessageCommand(CoreNetworkServer server, CoreNetworkOpcode op, byte[] data)
        {
            //Decode command
            var command = DecodeRPCMessageCommand(server, data);

            //Log
            delta.Log("RPCNetwork-OnMessageRPCMessageCommand", $"Got RPC Message; opcode={command.opcode}, is_compressed={command.is_compressed.ToString()}, payload(length)={command.payload.Length}", LibDeltaSystem.DeltaLogLevel.Debug);

            //Queue
            Program.queue.Enqueue(command);

            return new byte[0];
        }

        private IRPCGroupQuery DecodeFilter(byte filterType, byte[] payload, int offset, out ObjectId? targetServerId)
        {
            IRPCGroupQuery query;
            if (filterType == 0)
            {
                query = new RPCGroupQueryUser
                {
                    user_id = BinaryTool.ReadMongoID(payload, offset)
                };
                targetServerId = null;
            }
            else if (filterType == 1)
            {
                query = new RPCGroupQueryUser
                {
                    user_id = BinaryTool.ReadMongoID(payload, offset)
                };
                targetServerId = BinaryTool.ReadMongoID(payload, offset + 12);
            }
            else if (filterType == 2)
            {
                query = new RPCGroupQueryServer
                {
                    server_id = BinaryTool.ReadMongoID(payload, offset)
                };
                targetServerId = BinaryTool.ReadMongoID(payload, offset);
            }
            else if (filterType == 3)
            {
                query = new RPCGroupQueryServerTribe
                {
                    server_id = BinaryTool.ReadMongoID(payload, offset),
                    tribe_id = BinaryTool.ReadInt32(payload, offset + 12)
                };
                targetServerId = BinaryTool.ReadMongoID(payload, offset);
            }
            else
            {
                throw new Exception("Invalid filter type.");
            }
            return query;
        }

        private RPCInternalMessage DecodeRPCMessageCommand(CoreNetworkServer server, byte[] payload)
        {
            //Read header
            byte filterType = payload[0];
            byte filterDataLength = payload[1];
            byte actionType = payload[2 + filterDataLength];
            int rpcOpcode = BinaryTool.ReadInt32(payload, 2 + filterDataLength + 1);
            ushort actionDataLength = BinaryTool.ReadUInt16(payload, 2 + filterDataLength + 5);
            ushort actionDataCompressedLength = BinaryTool.ReadUInt16(payload, 2 + filterDataLength + 7);
            bool isCompressed = actionDataLength > 255;

            //Read the filter
            var query = DecodeFilter(filterType, payload, FILTER_OFFSET, out ObjectId? targetServerId);

            //Validate type
            if (actionType != 0x00)
                throw new Exception("Invalid action type.");

            //Read the payload
            byte[] actionPayload = new byte[actionDataCompressedLength];
            Array.Copy(payload, 2 + filterDataLength + 9, actionPayload, 0, actionDataCompressedLength);

            //Create the internal message
            return new RPCInternalMessage
            {
                is_compressed = isCompressed,
                opcode = rpcOpcode,
                payload = actionPayload,
                query = query,
                source_server = server,
                target_server = targetServerId,
                payload_size_decompressed = actionDataLength
            };
        }
    }
}
