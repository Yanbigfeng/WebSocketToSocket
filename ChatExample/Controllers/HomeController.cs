using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ChatExample.Models;
using System.Net.Sockets;

namespace ChatExample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        #region 控制器群发
        public void WebSocket()
        {

            new WebSocketService();
        }
        #endregion


    }
}