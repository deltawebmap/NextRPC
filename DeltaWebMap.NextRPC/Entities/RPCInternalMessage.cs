using DeltaWebMap.NextRPC.Queries;
using LibDeltaSystem.CoreHub.CoreNetwork;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DeltaWebMap.NextRPC.Entities
{
    /// <summary>
    /// An internal message
    /// </summary>
    public class RPCInternalMessage
    {
        public bool is_compressed;
        public byte[] payload;
        public int payload_size_decompressed;
        public IRPCGroupQuery query;
        public int opcode;
        public ObjectId? target_server;
        public CoreNetworkServer source_server;

        public byte[] GetDecompressedPayload()
        {
            if(is_compressed)
            {
                byte[] payload_decompressed = new byte[payload_size_decompressed];
                using(MemoryStream ms = new MemoryStream(payload))
                using(GZipStream gz = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    gz.Read(payload_decompressed, 0, payload_size_decompressed);
                }
                return payload_decompressed;
            } else
            {
                return payload;
            }
        }

        public RPCExternalMessage GetExternalMessage()
        {
            return new RPCExternalMessage
            {
                opcode = opcode,
                target_server = target_server?.ToString(),
                source = source_server.type.ToString() + "@" + source_server.id,
                payload = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(GetDecompressedPayload()))
            };
        }
    }
}
