using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Diagnostics;

namespace ChatExample.Models
{
    public class SocketServer
    {
        private static byte[] result = new byte[1024];
        private static int myProt = 8885;   //端口  
        static Socket serverSocket;  //服务器服务
        //建立登录用户记录信息
        public static Dictionary<string, Socket> ListUser = new Dictionary<string, Socket>();

        #region 开启服务
        public void Start()
        {
            //服务器IP地址  
            IPAddress ip = IPAddress.Parse("192.168.1.25");
            //socket的构造函数进行服务注册
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //绑定IP地址：端口  
            serverSocket.Bind(new IPEndPoint(ip, myProt));
            //设定最多10个排队连接请求 
            serverSocket.Listen(10);
            Debug.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());
            //通过Clientsoket发送数据  
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
            Console.ReadLine();
        }
        #endregion

        #region 开启监听
        /// <summary>  
        /// 监听客户端连接  
        /// </summary>  
        private void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                //在这里绝对不能使用服务给客户端发送消息
                Thread receiveThread = new Thread(ReceiveMessage);
                receiveThread.Start(clientSocket);
            }
        }
        #endregion

        #region 接受消息
        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name="clientSocket"></param>  
        private void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    //通过clientSocket接收数据  
                    int receiveNumber = myClientSocket.Receive(result);
                    //  websocket建立连接的时候，除了TCP连接的三次握手，websocket协议中客户端与服务器想建立连接需要一次额外的握手动作
                    string msg = Encoding.UTF8.GetString(result, 0, receiveNumber);
                    var buffer = result;
                    if (msg.Contains("Sec-WebSocket-Key"))
                    {

                        myClientSocket.Send(PackageHandShakeData(buffer, receiveNumber));

                        continue;
                    }
                    var resultStr = AnalyzeClientData(result, receiveNumber);
                    string[] resultList = resultStr.Split(',');
                    //string sendMsg = $"你({myClientSocket.RemoteEndPoint.ToString()})：" + resultList[1] + "【服务端回复】";
                    //myClientSocket.Send(SendMsg(sendMsg));//取消对自己提示发送给别人
                    if (string.IsNullOrEmpty(resultList[0]))
                    {
                        //退出                       
                        SignOut(myClientSocket.RemoteEndPoint.ToString());
                        ListUser.Remove(myClientSocket.RemoteEndPoint.ToString());
                        myClientSocket.Shutdown(SocketShutdown.Both);
                        myClientSocket.Close();
                        Debug.WriteLine("当前退出用户：" + myClientSocket.RemoteEndPoint.ToString());
                    }
                    else if (resultList[0] == "login")
                    {
                        //登录
                        Login(myClientSocket.RemoteEndPoint.ToString());
                        ListUser.Add(myClientSocket.RemoteEndPoint.ToString(), myClientSocket);
                        Debug.WriteLine("当前登录用户：" + myClientSocket.RemoteEndPoint.ToString());
                    }
                    else if (resultList[0] == "all")
                    {
                        //群发所有用户
                        GroupChat(myClientSocket.RemoteEndPoint.ToString(), resultList[1]);
                    }
                    else if (resultList[0] == "groupA")
                    {
                        //群组发送
                        GroupChatA("groupA", myClientSocket.RemoteEndPoint.ToString(), resultList[1]);
                    }
                    else if (resultList[0] == "groupB")
                    {
                        //群组发送
                        GroupChatA("groupB", myClientSocket.RemoteEndPoint.ToString(), resultList[1]);
                    }
                    else
                    {
                        //单聊
                        SingleChat(myClientSocket.RemoteEndPoint.ToString(), resultList[0], resultList[1]);
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //myClientSocket.Shutdown(SocketShutdown.Both);
                    //myClientSocket.Close();
                    break;
                }
            }
        }
        #endregion    

        #region 打包请求连接数据
        /// <summary>
        /// 打包请求连接数据
        /// </summary>
        /// <param name="handShakeBytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private byte[] PackageHandShakeData(byte[] handShakeBytes, int length)
        {
            string handShakeText = Encoding.UTF8.GetString(handShakeBytes, 0, length);
            string key = string.Empty;
            Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = reg.Match(handShakeText);
            if (m.Value != "")
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }
            byte[] secKeyBytes = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            string secKey = Convert.ToBase64String(secKeyBytes);
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
            responseBuilder.Append("Upgrade: websocket" + "\r\n");
            responseBuilder.Append("Connection: Upgrade" + "\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKey + "\r\n\r\n");
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
        #endregion

        #region 处理接收的数据
        /// <summary>
        /// 处理接收的数据
        /// 参考 http://www.cnblogs.com/smark/archive/2012/11/26/2789812.html
        /// </summary>
        /// <param name="recBytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string AnalyzeClientData(byte[] recBytes, int length)
        {
            int start = 0;
            // 如果有数据则至少包括3位
            if (length < 2) return "";
            // 判断是否为结束针
            bool IsEof = (recBytes[start] >> 7) > 0;
            // 暂不处理超过一帧的数据
            if (!IsEof) return "";
            start++;
            // 是否包含掩码
            bool hasMask = (recBytes[start] >> 7) > 0;
            // 不包含掩码的暂不处理
            if (!hasMask) return "";
            // 获取数据长度
            UInt64 mPackageLength = (UInt64)recBytes[start] & 0x7F;
            start++;
            // 存储4位掩码值
            byte[] Masking_key = new byte[4];
            // 存储数据
            byte[] mDataPackage;
            if (mPackageLength == 126)
            {
                // 等于126 随后的两个字节16位表示数据长度
                mPackageLength = (UInt64)(recBytes[start] << 8 | recBytes[start + 1]);
                start += 2;
            }
            if (mPackageLength == 127)
            {
                // 等于127 随后的八个字节64位表示数据长度
                mPackageLength = (UInt64)(recBytes[start] << (8 * 7) | recBytes[start] << (8 * 6) | recBytes[start] << (8 * 5) | recBytes[start] << (8 * 4) | recBytes[start] << (8 * 3) | recBytes[start] << (8 * 2) | recBytes[start] << 8 | recBytes[start + 1]);
                start += 8;
            }
            mDataPackage = new byte[mPackageLength];
            for (UInt64 i = 0; i < mPackageLength; i++)
            {
                mDataPackage[i] = recBytes[i + (UInt64)start + 4];
            }
            Buffer.BlockCopy(recBytes, start, Masking_key, 0, 4);
            for (UInt64 i = 0; i < mPackageLength; i++)
            {
                mDataPackage[i] = (byte)(mDataPackage[i] ^ Masking_key[i % 4]);
            }
            return Encoding.UTF8.GetString(mDataPackage);
        }
        #endregion

        #region 发送数据
        /// <summary>
        /// 把发送给客户端消息打包处理（拼接上谁什么时候发的什么消息）
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="message">Message.</param>
        private byte[] SendMsg(string msg)
        {
            byte[] content = null;
            byte[] temp = Encoding.UTF8.GetBytes(msg);
            if (temp.Length < 126)
            {
                content = new byte[temp.Length + 2];
                content[0] = 0x81;
                content[1] = (byte)temp.Length;
                Buffer.BlockCopy(temp, 0, content, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                content = new byte[temp.Length + 4];
                content[0] = 0x81;
                content[1] = 126;
                content[2] = (byte)(temp.Length & 0xFF);
                content[3] = (byte)(temp.Length >> 8 & 0xFF);
                Buffer.BlockCopy(temp, 0, content, 4, temp.Length);
            }
            return content;
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
                        Socket socket = item.Value;
                        try
                        {
                            socket.Send(SendMsg($"用户（{userId}）登录了"));
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
                        Socket socket = item.Value;
                        try
                        {
                            socket.Send(SendMsg($"用户（{userId}）退出了"));
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
            Socket socket = ListUser[userIdB];
            if (socket != null)
            {
                try
                {
                    socket.Send(SendMsg($"用户（{userIdA}=>{userIdB}）:{msg}"));
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
                        Socket socket = item.Value;
                        try
                        {
                            socket.Send(SendMsg($"用户（{userId}=>{item.Key}）:{msg}"));
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
                Socket socket = ListUser[itemG.userId];
                try
                {
                    socket.Send(SendMsg($"用户（{userId}=>{itemG.userId}）:{msg}"));
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

    }
}