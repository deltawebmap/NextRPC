using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC.SockCommands
{
    public class RefreshGroupsRequestCommand : ISockCommand
    {
        public async Task HandleCommand(RPCConnection conn)
        {
            await conn.RefreshGroups();
        }
    }
}
