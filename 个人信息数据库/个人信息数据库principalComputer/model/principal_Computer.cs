using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace 个人信息数据库principalComputer.model
{
    /// <summary>
    /// 上位机
    /// </summary>
    public class principal_Computer
    {
        public principal_Computer(System.Action<string> reminder, System.Action<int , ecommand , string> switchimplement)
        {
            this.reminder = reminder;
            this.switchimplement = switchimplement;
            this.ReceiveAction = str =>
            {
                //reminder(str);
                implement(str);
            };

            ServerSocket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;
            ServerInfo = new IPEndPoint(ip , port);
            ServerSocket.Bind(ServerInfo);//将SOCKET接口和IP端口绑定
            ServerSocket.Listen(10);//开始监听，并且挂起数为10

            ClientSocket = new Socket[65535];//为客户端提供连接个数
            //MsgBuffer = new byte[65535];//消息数据大小
            ClientNumb = 0;//数量从0开始统计

            ServerThread = new Thread(new ThreadStart(RecieveAccept));//将接受客户端连接的方法委托给线程
            ServerThread.IsBackground = true;
            ServerThread.Start();//线程开始运行

            reminder("运行上位机");
        }

        public void send(string str)
        {
            byte[] MsgBuffer = encoding.GetBytes(str);
            for (int i = 0; i < ClientNumb; i++)
            {
                if (ClientSocket[i].Connected)
                {
                    //回发数据到客户端
                    ClientSocket[i].Send(MsgBuffer , 0 , MsgBuffer.Length , SocketFlags.None);
                }
            }
        }

        public void send(int id , string str)
        {
            byte[] buffer = encoding.GetBytes(str);
            if (ClientSocket[id].Connected)
            {
                //回发数据到客户端
                ClientSocket[id].Send(buffer , 0 , buffer.Length , SocketFlags.None);
            }
        }
        //接受客户端连接的方法
        private void RecieveAccept()
        {
            while (true)
            {
                //Accept 以同步方式从侦听套接字的连接请求队列中提取第一个挂起的连接请求，然后创建并返回新的 Socket。
                //在阻止模式中，Accept 将一直处于阻止状态，直到传入的连接尝试排入队列。连接被接受后，原来的 Socket 继续将传入的连接请求排入队列，直到您关闭它。
                byte[] buffer = new byte[65535];
                ClientSocket[ClientNumb] = ServerSocket.Accept();
                ClientSocket[ClientNumb].BeginReceive(buffer , 0 , buffer.Length , SocketFlags.None ,
                new AsyncCallback(RecieveCallBack) , ClientSocket[ClientNumb]);
                //lock (this.ClientList)
                //{
                //    this.ClientList.Items.Add(ClientSocket[ClientNumb].RemoteEndPoint.ToString() + " 成功连接服务器.");
                //}
                ReceiveAction(encoding.GetString(buffer));
                分配id(ClientNumb);
                ClientNumb++;
            }
        }

        //回发数据给客户端
        private void RecieveCallBack(IAsyncResult AR)
        {
            try
            {
                Socket RSocket = (Socket)AR.AsyncState;
                //int REnd = RSocket.EndReceive(AR);
                //对每一个侦听的客户端端口信息进行接收
                //for (int i = 0; i < ClientNumb; i++)
                {
                    //if (ClientSocket[i].Connected)
                    //{
                    //    //回发数据到客户端
                    //    ClientSocket[i].Send(MsgBuffer , 0 , REnd , SocketFlags.None);
                    //}
                    byte[] MsgBuffer = new byte[65535];
                    //同时接收客户端回发的数据，用于回发
                    RSocket.BeginReceive(MsgBuffer , 0 , MsgBuffer.Length , SocketFlags.None , ar =>
                    {
                        //对方断开连接时, 这里抛出Socket Exception
                        //An existing connection was forcibly closed by the remote host 
                        try
                        {

                            RSocket.EndReceive(ar);
                            ReceiveAction(encoding.GetString(MsgBuffer).Trim('\0' , ' '));
                            RecieveCallBack(ar);
                        }
                        catch (SocketException e)
                        {
                            reminder("对方断开连接 " + e.Message);
                        }

                    } , RSocket);
                }
            }
            catch { }

        }

        private IPEndPoint ServerInfo;//存放服务器的IP和端口信息
        private Socket ServerSocket;//服务端运行的SOCKET
        private Thread ServerThread;//服务端运行的线程
        private Socket[] ClientSocket;//为客户端建立的SOCKET连接
        private int ClientNumb;//存放客户端数量
        private int port = 54321; //端口号
        private System.Action<string> ReceiveAction;
        private System.Action<string> reminder;
        private Encoding encoding = Encoding.Default;
        public System.Action<int,ecommand,string> switchimplement
        {
            set;
            get;
        }

        private void implement(string str)
        {
            try
            {
                str = str.TrimEnd('\0');
                if (switchimplement != null && !string.IsNullOrEmpty(str))
                {
                    ctransmitter transmitter = JsonConvert.DeserializeObject<ctransmitter>(str);
                ecommand command = (ecommand)Enum.Parse(typeof(ecommand) , transmitter.command);
                    int id = Convert.ToInt32(transmitter.id);
                    switchimplement(id,command,transmitter.str);
                }
                //switch (command)
                //{
                //    case ecommand.ce:
                //        reminder("收到" + transmitter.id);
                //        break;
                //}
            }
            catch(Exception e)
            {
                reminder("str不是ctransmitter " + e.Message);
            }
        }

        private void 分配id(int id)
        {
            send(id , new ctransmitter(-1 , ecommand.id , id.ToString()).ToString());
            reminder(id.ToString() + "连接");
        }
        
    }
}
