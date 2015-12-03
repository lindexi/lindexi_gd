using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace 个人信息数据库.model
{
    /// <summary>
    /// 下位机
    /// </summary>
    public class slaveComputer
    {
        public slaveComputer(System.Action<string> reminder, System.Action<int , ecommand , string> switchimplement)
        {    
            this.reminder = reminder;
            this.switchimplement = switchimplement;
            this.ReceiveAction = str =>
            {
                //reminder(str);
                implement(str);
            };

            //定义一个IPV4，TCP模式的Socket
            ClientSocket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            //MsgBuffer = new Byte[65535];
            //MsgSend = new Byte[65535];
            
            reminder("下位机");
        }
        public System.Action<int , ecommand , string> switchimplement
        {
            set;
            get;
        }
        public void access(string ip)
        {
            this.ip = ip;
            ServerInfo = new IPEndPoint(IPAddress.Parse(ip) , port);
            //客户端连接服务端指定IP端口，Sockket
            ClientSocket.Connect(ServerInfo);
            //将用户登录信息发送至服务器，由此可以让其他客户端获知
            //ClientSocket.Send(encoding.GetBytes(" 进入系统！\n"));
            //send("进入系统");            

            send(new ctransmitter(-1,ecommand.login,"进入系统").ToString());
            //开始从连接的Socket异步读取数据。接收来自服务器，其他客户端转发来的信息
            //AsyncCallback引用在异步操作完成时调用的回调方法
            //ClientSocket.BeginReceive(MsgBuffer , 0 , MsgBuffer.Length , SocketFlags.None , new AsyncCallback(ReceiveCallBack) , null);
            ReceiveCallBack(ReceiveAction);

            reminder("连接");
        }

        public void exit()
        {
            if (ClientSocket.Connected)
            {
                ClientSocket.Send(Encoding.Unicode.GetBytes("离开了房间！\n"));
                //禁用发送和接受
                ClientSocket.Shutdown(SocketShutdown.Both);
                //关闭套接字，不允许重用
                ClientSocket.Disconnect(false);
            }
            ClientSocket.Close();
        }
        public void send(string str)
        {
            if (ClientSocket.Connected)
            {
                byte[] buffer = encoding.GetBytes(str);
                ClientSocket.Send(buffer , 0 , buffer.Length , SocketFlags.None);
            }
            else
            {
                try
                {
                    access(ip);                   
                }
                catch(SocketException e)
                {
                    reminder ("连接失败 " + e.Message);
                }
                catch(InvalidOperationException e)
                {
                    reminder("服务器没有开启"+e.Message);
                }
            }
        }
        private IPEndPoint ServerInfo;
        private Socket ClientSocket;      
        private Encoding encoding = Encoding.Default;
        private System.Action<string> ReceiveAction;
        private System.Action<string> reminder;
        private string ip
        {
            set;
            get;
        }
        private void ReceiveCallBack(System.Action<string> ReceiveAction)
        {
            try
            {
                //结束挂起的异步读取，返回接收到的字节数。 AR，它存储此异步操作的状态信息以及所有用户定义数据                
                byte[] MsgBuffer = new byte[65535];
                ClientSocket.BeginReceive(MsgBuffer , 0 , MsgBuffer.Length , SocketFlags.None , ar =>
                {
                    //对方断开连接时, 这里抛出Socket Exception
                    //An existing connection was forcibly closed by the remote host 
                    try
                    {
                        ClientSocket.EndReceive(ar);
                        ReceiveAction(encoding.GetString(MsgBuffer).Trim('\0' , ' '));
                        ReceiveCallBack(ReceiveAction);
                    }
                    catch (SocketException e)
                    {
                        reminder("对方断开连接" + e.Message);
                    }
                    catch(ObjectDisposedException e)
                    {
                        reminder (id.ToString() + "连接退出" + e.Message);
                        return;
                    }

                } /*new AsyncCallback(ReceiveCallBack)*/ , null);
            }
            catch
            {
                reminder("ReceiveCallBack 错误");
            }
        }
        private int port = 54321; //端口号
        public int id
        {
            set;
            get;
        }
        private Random ran
        {
            set;
            get;
        } = new Random();
        private void implement(string str)
        {
            try
            {
                ctransmitter transmitter = JsonConvert.DeserializeObject<ctransmitter>(str);
                ecommand command = (ecommand)Enum.Parse(typeof(ecommand) , transmitter.command);
                int id = Convert.ToInt32(transmitter.id);
                switchimplement(id , command , transmitter.str);
            }
            catch (Exception e)
            {
                reminder("slaveComputer.implement:str不是ctransmitter " + e.Message);
                reminder(str);
            }
        }        
    }
}
