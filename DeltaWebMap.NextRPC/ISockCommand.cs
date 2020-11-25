using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaWebMap.NextRPC
{
    public interface ISockCommand
    {
        Task HandleCommand(RPCConnection conn);
    }
}
