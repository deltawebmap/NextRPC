using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC
{
    public interface ISockFilter
    {
        bool CheckFilter(RPCConnection connection);
    }
}
