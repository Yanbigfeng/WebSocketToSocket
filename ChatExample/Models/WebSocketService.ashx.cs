using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Web;
using System.Web.WebSockets;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace ChatExample.Models
{
    /// <summary>
    /// WebSocket 的摘要说明
    /// </summary>
    public class WebSocketService : IHttpHandler
    {
        private static Dictionary<string, WebSocket> ListUser = new Dictionary<string, WebSocket>();//用户连接池
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(Accept);
            }
            else
            {


            }
        }

        #region 处理客户端连接请求
        /// <summary>
        /// 处理客户端连接请求
        /// </summary>
        /// <param name="result"></param>
        private async Task Accept(AspNetWebSocketContext context)
        {
            //创建新WebSocket实例
            WebSocket myClientSocket = context.WebSocket;
            string userId = context.QueryString["userId"];

            try
            {

                string descUser = string.Empty;//目的用户
                while (true)
                {
                    if (myClientSocket.State == WebSocketState.Open)
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                        WebSocketReceiveResult result = await myClientSocket.ReceiveAsync(buffer, CancellationToken.None);

                        #region 消息处理（字符截取、消息转发）
                        try
                        {
                            #region 关闭Socket处理，删除连接池
                            if (myClientSocket.State != WebSocketState.Open)//连接关闭
                            {
                                if (ListUser.ContainsKey(userId)) ListUser.Remove(userId);//删除连接池
                                break;
                            }
                            #endregion
                            string userMsg = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);//发送过来的消息
                            string[] resultList = userMsg.Split(',');
                            if (string.IsNullOrEmpty(resultList[0]))
                            {
                                //退出                       
                                SignOut(userId);
                                ListUser.Remove(userId);
                                Debug.WriteLine("当前退出用户：" + userId);
                            }
                            else if (resultList[0] == "login")
                            {
                                //登录
                                Login(userId);
                                #region 用户添加连接池
                                //第一次open时，添加到连接池中
                                if (!ListUser.ContainsKey(userId))
                                    ListUser.Add(userId, myClientSocket);//不存在，添加
                                else
                                    if (myClientSocket != ListUser[userId])//当前对象不一致，更新
                                    ListUser[userId] = myClientSocket;
                                #endregion
                                Debug.WriteLine("当前登录用户：" + userId);
                            }
                            else if (resultList[0] == "all")
                            {
                                //群发所有用户
                                GroupChat(userId, resultList[1]);
                            }
                            else if (resultList[0] == "groupA")
                            {
                                //群组发送
                                GroupChatA("groupA", userId, resultList[1]);
                            }
                            else if (resultList[0] == "groupB")
                            {
                                //群组发送
                                GroupChatA("groupB", userId, resultList[1]);
                            }
                            else
                            {
                                //单聊
                                SingleChat(userId, resultList[0], resultList[1]);
                            }

                        }
                        catch (Exception exs)
                        {
                            //消息转发异常处理，本次消息忽略 继续监听接下来的消息
                        }
                        #endregion
                    }
                    else
                    {
                        break;
                    }
                }//while end
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.ToString());
            }
        }
        #endregion


        /*************************************************
       * 以下为任意封装方法为了达到自己目的即可
       * **********************************************/

        #region  登录提示别人
        public void Login(string userId)
        {
            if (ListUser.Count() > 0)
            {
                foreach (var item in ListUser)
                {
                    if (item.Key != userId)
                    {
                        WebSocket socket = item.Value;

                        try
                        {
                            if (socket != null && socket.State == WebSocketState.Open)
                            {
                                ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"用户（{userId}）登录了"));
                                socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("该用户已掉线：" + item.Key);
                            //用户已掉线就删除掉
                            ListUser.Remove(item.Key);
                        }
                    }
                }

            }

        }
        #endregion

        #region  退出提示别人
        public void SignOut(string userId)
        {
            if (ListUser.Count() > 0)
            {
                foreach (var item in ListUser)
                {
                    if (item.Key != userId)
                    {
                        WebSocket socket = item.Value;
                        try
                        {
                            if (socket != null && socket.State == WebSocketState.Open)
                            {
                                ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"用户（{userId}）退出了"));
                                socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("该用户已掉线：" + item.Key);
                            //用户已掉线就删除掉
                            ListUser.Remove(item.Key);
                        }
                    }
                }

            }

        }
        #endregion

        #region 单聊
        public void SingleChat(string userIdA, string userIdB, string msg)
        {
            WebSocket socket = ListUser[userIdB];
            if (socket != null)
            {                         
                try
                {
                    if (socket != null && socket.State == WebSocketState.Open)
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"用户（{userIdA}=>{userIdB}）:{msg}"));
                        socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("该用户已掉线：" + userIdB);
                    //用户已掉线就删除掉
                    ListUser.Remove(userIdB);
                }
            }

        }
        #endregion

        #region 群发
        public void GroupChat(string userId, string msg)
        {
            if (ListUser.Count() > 0)
            {
                foreach (var item in ListUser)
                {
                    if (item.Key != userId)
                    {
                        WebSocket socket = item.Value;
                        try
                        {
                            if (socket != null && socket.State == WebSocketState.Open)
                            {
                                ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"用户（{userId}=>{item.Key}）:{msg}"));
                                socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("该用户已掉线：" + item.Key);
                            //用户已掉线就删除掉
                            ListUser.Remove(item.Key);
                        }
                    }
                }

            }

        }
        #endregion

        #region 实现群组

        //群组记录分类
        List<GroupHelp> groupList = new List<GroupHelp>();
        public void GroupChatA(string groupName, string userId, string msg)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return;
            }
            //判断自己是否在群组
            GroupHelp isEisx = groupList.Where(b => b.userId == userId && b.Name == groupName).FirstOrDefault();
            if (isEisx == null)
            {
                groupList.Add(new GroupHelp()
                {
                    Name = groupName,
                    userId = userId
                });
            }
            //根据群组名称判断是否存在群组
            var nowGroupList = groupList.Where(b => b.Name == groupName).ToList();
            foreach (var itemG in nowGroupList)
            {
                WebSocket socket = ListUser[itemG.userId];
                try
                {
                    if (socket != null && socket.State == WebSocketState.Open)
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"用户（{userId}=>{itemG.userId}）:{msg}"));
                        socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("该用户已掉线：" + itemG.userId);
                    //用户已掉线就删除掉
                    ListUser.Remove(itemG.userId);
                }
            }
        }
        #endregion

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}