using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Net;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime;
using System.Runtime.InteropServices;

namespace LanChat
{
    public partial class Form1 : Form
    {
        public Adapter adapter;
        public List<Device> deviceList;   //设备列表
        public List<string> ipList;
        public List<ChatForm> chatForms;
        public Form1(Adapter adapter)
        {
            InitializeComponent();
            deviceList = new List<Device>();
            ipList = new List<string>();
            chatForms = new List<ChatForm>();
            this.adapter = adapter;
            toolStripStatusLabel2.Text =adapter.Name + "   " + adapter.ip;


                 

            listView1.Columns.Add("ip地址", 120, HorizontalAlignment.Left);
            listView1.Columns.Add("mac地址", 120, HorizontalAlignment.Left);
            listView1.Columns.Add("未读消息", 120, HorizontalAlignment.Left);




           

        }

        //收到checkOnline请求报告自己在线情况
        public void responseOnline()
        {
            IPAddress ip = IPAddress.Any;
            IPEndPoint ipe = new IPEndPoint(ip, 8848);
            Socket serv_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serv_socket.Bind(ipe);
            bool isE = false;
            while(!isE)
            {
                isE = serv_socket.Poll(-1, SelectMode.SelectRead);
                if(isE)
                {
                    byte[] buf = new byte[8];
                    IPEndPoint receiver = new IPEndPoint(IPAddress.Any, 28001);
                    EndPoint endPoint = (EndPoint)receiver;
                    int bytelen=serv_socket.ReceiveFrom(buf, 8, SocketFlags.None, ref endPoint);  //收到的消息是发送端的广播地址
                    if(bytelen!=0)
                    {
                        IPAddress remote_ip = ((System.Net.IPEndPoint)endPoint).Address;
                        long address = BitConverter.ToInt64(buf, 0);
                        //不处理自己发的广播包和与当前网卡不在同一局域网的包
                        if (remote_ip.Address != adapter.ip.Address&&
                            address== convertToBroadcastAddress(adapter.ip, adapter.ipSubnet).Address)   
                        {
                            IPAddress local_ip = adapter.ip;
                            Console.WriteLine("responseThread_sorceIP:" + remote_ip);
                            Console.WriteLine("i will send you my IP:" + local_ip);
                            IPEndPoint iep = new IPEndPoint(remote_ip, 8849);
                            //发送自己的信息给请求者
                            byte[] buffer = SerializeObject(new OnlineInfo { ip = local_ip, mac = adapter.mac.ToString() });
                            serv_socket.SendTo(buffer, iep);                          
                        }
                    }
                    isE = false;
                }
            }
        }
        //发出查找在线终端请求
        public void sendCheckOnline()
        {
            IPAddress ip = convertToBroadcastAddress(adapter.ip, adapter.ipSubnet);
            IPEndPoint ipe = new IPEndPoint(ip, 8848);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);   //设置广播
            byte[] buff = BitConverter.GetBytes(ip.Address);
            
            socket.SendTo(buff, ipe); //把广播地址发送
            
            Console.WriteLine("send OK:"+ip.ToString());
            socket.Close();
        }
        //接受在线信息
        public void acceptOnlineInfo()
        {
            IPAddress ip = IPAddress.Any;
            IPEndPoint ipe = new IPEndPoint(ip, 8849);
            Socket serv_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serv_socket.Bind(ipe);
            bool isE = false;
            while (!isE)
            {
                isE = serv_socket.Poll(-1, SelectMode.SelectRead);
                if (isE)
                {
                    byte[] buf = new byte[1024];
                    IPEndPoint receiver = new IPEndPoint(IPAddress.Any, 28001);
                    EndPoint endPoint = (EndPoint)receiver;
                    int bytelen = serv_socket.ReceiveFrom(buf, 1024, SocketFlags.None, ref endPoint);
                    if (bytelen != 0)
                    {
                        OnlineInfo info = (OnlineInfo)DeserializeObject(buf);
                        IPAddress remote_ip = info.ip;
                        Console.WriteLine("acceptThread_sorceIP:" + remote_ip);
                        Console.WriteLine("acceptThread_sorceIP:" + info.mac);
                        //判断是否存在于设备列表
                        if (!isEstInDevices(remote_ip))   //如果不存在，则添加新设备
                        {
                            Device d = new Device();
                            d.ip = remote_ip;
                            d.mac = info.mac;
                            d.isOnline = true;
                            deviceList.Add(d);
                        }
                        else   //如果存在，把状态改为在线,更新一下信息
                        {
                            Device d = findDevice(remote_ip.ToString());
                            d.isOnline = true;
                            d.mac = info.mac;
                        }
                        updateListView();
                       
                    }
                    isE = false;
                }
            }
        }
        //接受消息
        public void getMessage()
        {
            IPAddress ip = IPAddress.Any;
            IPEndPoint ipe = new IPEndPoint(ip, 8850);
            Socket serv_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serv_socket.Bind(ipe);
            serv_socket.Listen(100);
            List<Socket> socks = new List<Socket>();
            socks.Add(serv_socket);
            while (true)
            {
                List<Socket> temp = new List<Socket>(socks);
                Socket.Select(temp,null,null,-1);
                foreach(Socket sock in temp)
                {
                    if(sock==serv_socket)
                    {
                        Socket client_sock=sock.Accept();
                        Console.WriteLine("收到一个连接");
                        socks.Add(client_sock);
                    }
                    else
                    {
                        byte[] buf = new byte[1024];
                        int byte_len = sock.Receive(buf,1024,SocketFlags.None);
                        if(byte_len==0)
                        {
                            Console.WriteLine("一个连接关闭");
                            socks.Remove(sock);
                        }
                        else
                        {
                            Message msg = (Message)DeserializeObject(buf);
                            string r = chaneSring(Encoding.Unicode.GetString(msg.text));
                            
                            Console.WriteLine("收到一个消息:" + r);
                            Console.WriteLine("来自:" + msg.source.ToString());
                            Device d=findDevice(msg.source.ToString());  //有可能列表里还没有对应的设备
                            if (d == null)              
                            {
                                d = new Device();
                                d.ip = msg.source;
                                d.isOnline = true;
                                d.msgList.Add(msg);
                                deviceList.Add(d);
                            }
                            else
                            {
                                if(isEstInChatForms(msg.source.ToString()))  //是否已经打开了聊天窗口
                                {
                                    msg.hasReaded = true;
                                    findChatform(msg.source).showOneMessage(r,msg.time.ToLocalTime().ToString(),msg.source.ToString());
                                    d.msgList.Add(msg);
                                }
                                else
                                {
                                    d.msgList.Add(msg);
                                    if(d.mac!=null)
                                        updateListView();
                                }
                                
                            }
                        }
                    }
                }

            }
        }




        //将IP地址转化为广播地址
        public IPAddress convertToBroadcastAddress(IPAddress ip,IPAddress ipSubnet)
        {
            long not=~ipSubnet.Address;
            long bdct = ip.Address | not;
            byte[] temp = BitConverter.GetBytes(bdct);
            byte[] buf = new byte[8];
            for(int i=0;i<4;i++)
            {
                buf[i] = temp[i];
            }
            long re = BitConverter.ToInt64(buf,0);
            IPAddress address = new IPAddress(re);
            return address;
        }
        
        //指定的IP地址的设备是否已经存在于列表
        public bool isEstInDevices(IPAddress ip)
        {
            foreach(Device d in deviceList)
            {
                if (d.ip.Address == ip.Address)
                    return true;
            }
            return false;
        }
        //找到指定IP地址的设备
        public Device findDevice(string ip)
        {
            foreach(Device d in deviceList)
            {
                if (d.ip.ToString() == ip)
                    return d;
            }
            return null;
        }
        //统计未读消息
        public int countNoReadMsg(Device d)
        {
            int i = 0;
            foreach(Message m in d.msgList)
            {
                if (m.hasReaded == false)
                    i = i + 1;
            }
            return i;
        }
        //序列化相关
        public static byte[] SerializeObject(object obj)
        {
            if (obj == null)
                return null;
            //内存实例  
            MemoryStream ms = new MemoryStream();
            //创建序列化的实例  
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);//序列化对象，写入ms流中    
            byte[] bytes = new byte[1024];
            byte[] temp = ms.GetBuffer();
            for(int i=0;i< temp.Length; i++)
            {
                bytes[i] = temp[i];
            }
            return bytes;
        }
        public static object DeserializeObject(byte[] bytes)
        {
            object obj = null;
            if (bytes == null)
                return obj;
            //利用传来的byte[]创建一个内存流  
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            BinaryFormatter formatter = new BinaryFormatter();
            obj = formatter.Deserialize(ms);//把内存流反序列成对象    
            ms.Close();
            return obj;
        }
        //刷新在线列表
        private void refresh_Click(object sender, EventArgs e)
        {
            refreshOnline();
        }
        public void refreshOnline()
        {
            listView1.Items.Clear();
            foreach(Device d in deviceList)  //先改为全部不在线
            {
                d.isOnline = false;
            }
            sendCheckOnline();
        }


        //更新ListView
        public void updateListView()
        {
            while (!this.IsHandleCreated)
            {
                Console.WriteLine("wait hander..");
            }
            listView1.Invoke(new Action(() =>
            {
                listView1.Items.Clear();
            }));
            foreach (Device d in deviceList)
            {
                if(d.isOnline)
                {
                    ListViewItem lvi = new ListViewItem(d.ip.ToString());
                    lvi.SubItems.Add(d.mac.ToString());
                    lvi.SubItems.Add(countNoReadMsg(d).ToString());

                    while (!this.IsHandleCreated)
                    {
                        Console.WriteLine("wait hander..");
                    }
                    listView1.Invoke(new Action(() => {
                        listView1.Items.Add(lvi);
                    }));
                }
            }
        }

        //指定IP是否存在于打开聊天窗口列表中
        public bool isEstInChatForms(string ip)
        {
            foreach(ChatForm c in chatForms)
            {
                if(c.device.ip.ToString()==ip)
                {
                    return true;
                }
            }
            return false;
        }
        //找到指定IP对应的聊天窗口
        public ChatForm findChatform(IPAddress ip)
        {

            foreach (ChatForm c in chatForms)
            {
                if (c.device.ip.Address == ip.Address)
                {
                    return c;
                }
            }
            return null;
        }

        [Serializable]   //在线信息定义
        public struct OnlineInfo
        {
            public IPAddress ip;
            public string mac;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem lvi= listView1.SelectedItems[0];
            string ip = lvi.SubItems[0].Text;
            if(!isEstInChatForms(ip))
            {
                ChatForm chatForm = new ChatForm(findDevice(ip),this);
                chatForm.Owner = this;
                chatForms.Add(chatForm);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                Socket test = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                test.Bind(new IPEndPoint(adapter.ip, 132));


                Thread responseOnlineThread = new Thread(responseOnline);
                responseOnlineThread.IsBackground = true;
                responseOnlineThread.Start();
                Thread acceptOnlineThread = new Thread(acceptOnlineInfo);
                acceptOnlineThread.IsBackground = true;
                acceptOnlineThread.Start();


                Thread receviceMessageThread = new Thread(getMessage);
                receviceMessageThread.IsBackground = true;
                receviceMessageThread.Start();


                sendCheckOnline();

                test.Close();
            }
            catch
            {
                MessageBox.Show("不存在的网卡");
                this.Close();
            }


            
        }

        public string chaneSring(string s)
        {
            for(int i=0;i<s.Length;i++)
            {
                if (s[i] == '\0')
                    return s.Substring(0, i );
            }
            return s;
        }
    }
    //消息定义
    [Serializable]
    public class Message
    {
        public DateTime time;    //消息发送时间
        public byte[] text;   //发送文本
        public bool hasReaded=false;   //消息是否被读过
        public IPAddress source;    //消息发出方IP地址
        public bool isMe = false;   //是自己发送的消息还是远端发送的
    }

    //在线设备定义
    public class Device
    {
        public IPAddress ip;
        public string mac;
        public List<Message> msgList = new List<Message>();
        public bool isOnline;
    }

    


}
