using LibDeltaSystem;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.NextRPC
{
    public class RPCConnectionDefinition : DeltaWebServiceDefinition
    {
        public override string GetTemplateUrl()
        {
            return "/rpc/v1";
        }

        public override DeltaWebService OpenRequest(DeltaConnection conn, HttpContext e)
        {
            return new RPCConnection(conn, e);
        }
    }
}
