using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace 个人信息数据库.model
{   
    /// <summary>
    /// 上位机
    /// </summary>
    public class principalComputer
    {
        private int port = 54321; //端口号
        private IPAddress ip = IPAddress.Parse("10.20.30.40"); //ip地址
        private TcpListener tcpListener = null;
        private TcpClient tcpClient = null;
        private NetworkStream networkStream = null;
        private BinaryReader reader;
        //private BinaryWriter writer;
        private string getInfo = string.Empty;
        //开始监听
        private void btnStartListen_Click(object sender , EventArgs e)
        {
            tcpListener = new TcpListener(ip , port);
            tcpListener.Start(); //开始监听
            Thread acceptClientMsgThread = new Thread(AcceptClientMsg);
            //运行一个线程去处理客户端发来的信息
            acceptClientMsgThread.Start();
        }
        //处理客户端发来的信息
        private void AcceptClientMsg()
        {
            tcpClient = tcpListener.AcceptTcpClient();
            if (tcpClient != null)
            {
                networkStream = tcpClient.GetStream();
                reader = new BinaryReader(networkStream);
                while (true)
                {
                    getInfo += reader.ReadString();  //读取客户端发来的信息
                }
            }
        }
        //假如还要显示信息的话,可以整个显示按钮(当然最好的办法是用些线程)
        //然后点击button后让信息显示出来txtShowClientMsg.Text = getInfo;
        //如果服务器端想再给客户端发信息,就可以整个发送按钮.然后添加如下代码
        //string sendMsg = txtSendMsge.Text;
        //writer = new BinaryWriter( networkStream);
        //writer.write( sendMsg);
    }

    /// <summary>
    /// 下位机
    /// </summary>
    public class slaveComputer
    {
        private int port = 54321;
        private IPAddress ip = IPAddress.Parse("10.20.30.40");
        private TcpClient tcpClient = null;
        private NetworkStream networkStream = null;
        private BinaryReader reader;
        private BinaryWriter writer;
        //连接server
        private void StartConnect()
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(ip , port);
            networkStream = tcpClient.GetStream();
        }
        //发送信息
        private void send(string str)
        {
            string sendMsg = str;
            writer = new BinaryWriter(networkStream);
            writer.Write(sendMsg); //发送信息
        }
        //如果还要接受server的消息的话.
        public void Accept()
        {
            reader = new BinaryReader(networkStream);
            string getInfo = reader.ReadString();
        }
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
                    communicateSocket.EndReceive(ar);
                    ReceiveAction(Encoding.UTF8.GetString(msg).Trim('\0' , ' '));
                    Receive(ReceiveAction);
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
            serverSocket.Listen(1);
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
