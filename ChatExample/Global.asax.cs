using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ChatExample.Models;
using System.Threading;
using ChatExample.Models;

namespace ChatExample
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterHandler(RouteTable.Routes);  //webSocket做服务
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //socket实现
            SocketServer socket = new SocketServer();
            Thread myThread = new Thread(socket.Start);
            myThread.Start();

        }
        public static void RegisterHandler(RouteCollection routes)
        {

            RouteTable.Routes.Add("socket",
                 new Route("socket", new ChatExample.Models.PlainRouteHandler()));
        }

    }
}
