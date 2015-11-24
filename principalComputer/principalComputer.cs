using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace principalComputer
{
    /// <summary>
    /// 上位机
    /// </summary>
    public class principal_Computer
    {

        //private IPAddress ip = IPAddress.Parse("10.20.30.40"); //ip地址
        //private TcpListener tcpListener = null;
        //private TcpClient tcpClient = null;
        //private NetworkStream networkStream = null;
        //private BinaryReader reader;
        //private BinaryWriter writer;
        //private string getInfo = string.Empty;

        //开始监听
        //private void btnStartListen_Click(object sender , EventArgs e)
        //{
        //    tcpListener = new TcpListener(ip , port);
        //    tcpListener.Start(); //开始监听
        //    Thread acceptClientMsgThread = new Thread(AcceptClientMsg);
        //    //运行一个线程去处理客户端发来的信息
        //    acceptClientMsgThread.Start();
        //}
        //处理客户端发来的信息
        //private void AcceptClientMsg()
        //{
        //    tcpClient = tcpListener.AcceptTcpClient();
        //    if (tcpClient != null)
        //    {
        //        networkStream = tcpClient.GetStream();
        //        reader = new BinaryReader(networkStream);
        //        while (true)
        //        {
        //            getInfo += reader.ReadString();  //读取客户端发来的信息
        //        }
        //    }
        //}
        //假如还要显示信息的话,可以整个显示按钮(当然最好的办法是用些线程)
        //然后点击button后让信息显示出来txtShowClientMsg.Text = getInfo;
        //如果服务器端想再给客户端发信息,就可以整个发送按钮.然后添加如下代码
        //string sendMsg = txtSendMsge.Text;
        //writer = new BinaryWriter( networkStream);
        //writer.write( sendMsg);

        public principal_Computer(System.Action<string> ReceiveAction)
        {
            this.ReceiveAction = ReceiveAction;

            ServerSocket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;
            ServerInfo = new IPEndPoint(ip , port);
            ServerSocket.Bind(ServerInfo);//将SOCKET接口和IP端口绑定
            ServerSocket.Listen(10);//开始监听，并且挂起数为10

            ClientSocket = new Socket[65535];//为客户端提供连接个数
            //MsgBuffer = new byte[65535];//消息数据大小
            ClientNumb = 0;//数量从0开始统计

            ServerThread = new Thread(new ThreadStart(RecieveAccept));//将接受客户端连接的方法委托给线程
            ServerThread.Start();//线程开始运行

            ReceiveAction("运行上位机");
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
                            ReceiveAction("对方断开连接" + e.Message);
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
        //private byte[] MsgBuffer;//存放消息数据
        private int port = 54321; //端口号
        private System.Action<string> ReceiveAction;
        private Encoding encoding = Encoding.Default;

    }

    /// <summary>
    /// 下位机
    /// </summary>
    public class slaveComputer
    {
        public slaveComputer(System.Action<string> ReceiveAction)
        {
            this.ReceiveAction = ReceiveAction;
            //定义一个IPV4，TCP模式的Socket
            ClientSocket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            //MsgBuffer = new Byte[65535];
            //MsgSend = new Byte[65535];

            ReceiveAction("下位机");
        }
        public void access(string ip)
        {
            ServerInfo = new IPEndPoint(IPAddress.Parse(ip) , port);
            //客户端连接服务端指定IP端口，Sockket
            ClientSocket.Connect(ServerInfo);
            //将用户登录信息发送至服务器，由此可以让其他客户端获知
            ClientSocket.Send(encoding.GetBytes(" 进入系统！\n"));
            //开始从连接的Socket异步读取数据。接收来自服务器，其他客户端转发来的信息
            //AsyncCallback引用在异步操作完成时调用的回调方法
            //ClientSocket.BeginReceive(MsgBuffer , 0 , MsgBuffer.Length , SocketFlags.None , new AsyncCallback(ReceiveCallBack) , null);
            ReceiveCallBack(ReceiveAction);

            ReceiveAction("连接");
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
           byte[] buffer = encoding.GetBytes(str);
            //ClientSocket.Send(buffer);
            ClientSocket.Send(buffer , 0 , buffer.Length , SocketFlags.None);
        }
        private IPEndPoint ServerInfo;
        private Socket ClientSocket;
        //信息接收缓存
        //private Byte[] MsgBuffer;
        //信息发送存储
        //private Byte[] MsgSend;
        private Encoding encoding = Encoding.Default;
        private System.Action<string> ReceiveAction;
        private void ReceiveCallBack(System.Action<string> ReceiveAction)
        {
            try
            {
                //结束挂起的异步读取，返回接收到的字节数。 AR，它存储此异步操作的状态信息以及所有用户定义数据
                //int REnd = ClientSocket.EndReceive(AR);

                //lock (this.RecieveMsg)
                //{
                //    this.RecieveMsg.AppendText(Encoding.Unicode.GetString(MsgBuffer , 0 , REnd));
                //}
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
                        ReceiveAction("对方断开连接" + e.Message);
                    }

                } /*new AsyncCallback(ReceiveCallBack)*/ , null);

            }
            catch
            {
                //MessageBox.Show("已经与服务器断开连接！");
                //this.Close();
            }
        }
        private int port = 54321; //端口号


        //private int port = 54321;
        //private IPAddress ip = IPAddress.Parse("10.20.30.40");
        //private TcpClient tcpClient = null;
        //private NetworkStream networkStream = null;
        //private BinaryReader reader;
        //private BinaryWriter writer;
        ////连接server
        //private void StartConnect()
        //{
        //    tcpClient = new TcpClient();
        //    tcpClient.Connect(ip , port);
        //    networkStream = tcpClient.GetStream();
        //}
        ////发送信息
        //private void send(string str)
        //{
        //    string sendMsg = str;
        //    writer = new BinaryWriter(networkStream);
        //    writer.Write(sendMsg); //发送信息
        //}
        ////如果还要接受server的消息的话.
        //public void Accept()
        //{
        //    reader = new BinaryReader(networkStream);
        //    string getInfo = reader.ReadString();
        //}
    }

    public class principal_computer : notify_property
    {
        public principal_computer()
        {
            AccessAction = () =>
            {
                string friendIP = socket.communicateSocket.RemoteEndPoint.ToString();
                reminder = friendIP;
                try
                {
                    socket.Receive(ReceiveAction);
                }
                catch (Exception e)
                {
                    reminder = e.Message;
                }
            };

            ReceiveAction = msg =>
            {
                reminder = msg;
            };
        }

        public principal_computer(System.Action<string> ReceiveAction)
        {
            AccessAction = () =>
            {
                string friendIP = socket.communicateSocket.RemoteEndPoint.ToString();
                reminder = friendIP;
                try
                {
                    socket.Receive(ReceiveAction);
                }
                catch (Exception e)
                {
                    reminder = e.Message;
                }
            };

            this.ReceiveAction = ReceiveAction;
        }

        public void principal()
        {
            socket = new ServerSocket();
            socket.Access(string.Empty , AccessAction);
            reminder = "等待连接";

        }

        public void slave()
        {
            socket = new ClientSocket();
            string ip = "10.21.71.130";
            socket.Access(ip , AccessAction);
        }

        public void send(string str)
        {
            socket.Send(str);
        }

        SocketFunc socket;
        System.Action<string> ReceiveAction;
        System.Action AccessAction;


    }
    public class slave_computer
    {

    }
    /// <summary>
    /// SocketFunc是一个抽象类, 服务端和客户端只有建立连接的方法不同, 其它都相同, 所以把相同的部分放到这个类中
    /// </summary>
    public abstract class SocketFunc
    {
        //不管是服务端还是客户端, 建立连接后用这个Socket进行通信
        public Socket communicateSocket = null;

        //服务端和客户端建立连接的方式稍有不同, 子类会重载
        public abstract void Access(string IP , System.Action AccessAciton);

        //发送消息的函数
        public void Send(string message)
        {
            if (communicateSocket.Connected == false)
            {
                throw new Exception("还没有建立连接, 不能发送消息");
            }
            Byte[] msg = Encoding.UTF8.GetBytes(message);
            communicateSocket.BeginSend(msg , 0 , msg.Length , SocketFlags.None ,
                ar =>
                {

                } , null);
        }

        //接受消息的函数
        public void Receive(System.Action<string> ReceiveAction)
        {
            //如果消息超过1024个字节, 收到的消息会分为(总字节长度/1024 +1)条显示
            Byte[] msg = new byte[1024];
            //异步的接受消息
            communicateSocket.BeginReceive(msg , 0 , msg.Length , SocketFlags.None ,
                ar =>
                {
                    //对方断开连接时, 这里抛出Socket Exception
                    //An existing connection was forcibly closed by the remote host 
                    try
                    {

                        communicateSocket.EndReceive(ar);
                        ReceiveAction(Encoding.UTF8.GetString(msg).Trim('\0' , ' '));
                        Receive(ReceiveAction);
                    }
                    catch (SocketException e)
                    {
                        ReceiveAction("对方断开连接" + e.Message);
                    }

                } , null);
        }
    }

    public class ServerSocket : SocketFunc
    {
        //服务端重载Access函数
        public override void Access(string IP , System.Action AccessAciton)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            //本机预使用的IP和端口
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Any , 9050);
            //绑定服务端设置的IP
            serverSocket.Bind(serverIP);
            //设置监听个数
            serverSocket.Listen(10);
            //异步接收连接请求
            serverSocket.BeginAccept(ar =>
            {
                base.communicateSocket = serverSocket.EndAccept(ar);
                AccessAciton();
            } , null);
        }
    }

    public class ClientSocket : SocketFunc
    {
        //客户端重载Access函数
        public override void Access(string IP , System.Action AccessAciton)
        {
            base.communicateSocket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            base.communicateSocket.Bind(new IPEndPoint(IPAddress.Any , 9051));

            //服务器的IP和端口
            IPEndPoint serverIP;
            try
            {
                serverIP = new IPEndPoint(IPAddress.Parse(IP) , 9050);
            }
            catch
            {
                throw new Exception(String.Format("{0}不是一个有效的IP地址!" , IP));
            }

            //客户端只用来向指定的服务器发送信息,不需要绑定本机的IP和端口,不需要监听
            try
            {
                base.communicateSocket.BeginConnect(serverIP , ar =>
                {
                    AccessAciton();
                } , null);
            }
            catch
            {
                throw new Exception(string.Format("尝试连接{0}不成功!" , IP));
            }
        }
    }
}
