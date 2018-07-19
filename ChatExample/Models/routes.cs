using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace ChatExample.Models
{
    public class PlainRouteHandler : IRouteHandler
    {

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new WebSocketService();
        }
    }

    
}