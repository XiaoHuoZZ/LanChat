using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LanChat
{
    public partial class ChatForm : Form
    {
        public Device device;
        public Socket socket;
        public Form1 parent;
        public ChatForm(Device device,Form1 parent)
        {

            
            this.device = device;
            this.parent = parent;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(device.ip, 8850);
                InitializeComponent();
                this.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show("连接已关闭");
                this.parent.chatForms.Remove(this);
                parent.refreshOnline();
                this.Close();
            };
        }

        private void send_Click(object sender, EventArgs e)
        {
            try
            {
                string r = input_textBox.Text;
                if(r=="")
                {
                    MessageBox.Show("不能发送空消息！");
                }
                else
                {
                    byte[] temp = Encoding.Unicode.GetBytes(r);
                    if (temp.Length > 128)
                    {
                        MessageBox.Show("超过字数限制，最多64");
                    }
                    else
                    {
                        input_textBox.Text = "";
                        byte[] text = new byte[128];
                        for (int i = 0; i < temp.Length; i++)  //不够填充
                        {
                            text[i] = temp[i];
                        }
                        Message msg = new Message();
                        msg.text = text;
                        msg.time = System.DateTime.Now;
                        msg.source = parent.adapter.ip;
                        byte[] buf=SerializeObject(msg);
                        socket.Send(buf);
                        msg.hasReaded = true;
                        msg.isMe = true;
                        device.msgList.Add(msg);  //安全性？
                        showOneMessage(r,msg.time.ToLocalTime().ToString(),"我");
                    }
                }               
            }
            catch (Exception s)
            {
                MessageBox.Show("连接已关闭");
                this.parent.chatForms.Remove(this);
                parent.refreshOnline();
                this.Close();
            };

        }
        //显示一条消息
        public void showOneMessage(string text,string time,string user)
        {
            while (!this.IsHandleCreated)
            {
                Console.WriteLine("wait hander..");
            }
            output_TextBox.Invoke(new Action(() =>
            {
                int start = output_TextBox.Text.Length - 1;
                if (start == -1)
                    start = 0;
                string needColorString = "来自:" + user + "  " + time + "\r\n";
                int len = needColorString.Length - 1;
                output_TextBox.AppendText(needColorString + text + "\r\n");
                output_TextBox.Select(start, len);
                if(user=="我")
                    output_TextBox.SelectionColor = Color.Red;
                else
                    output_TextBox.SelectionColor = Color.Green;

            }));
            
        }
        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.parent.chatForms.Remove(this);
            socket.Close();
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
            for (int i = 0; i < temp.Length; i++)
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

        private void ChatForm_Load(object sender, EventArgs e)
        {


            this.Text = "正与" + device.ip.ToString() + "聊天中";
            foreach(Message msg in device.msgList)
            {
                if(msg.hasReaded==false)
                {
                    msg.hasReaded = true;
                }
                string r = chaneSring(Encoding.Unicode.GetString(msg.text));
                if(msg.isMe)
                    showOneMessage(r, msg.time.ToLocalTime().ToString(),"我");
                else
                    showOneMessage(r, msg.time.ToLocalTime().ToString(), msg.source.ToString());
            }
            parent.updateListView();







        }
        public string chaneSring(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\0')
                    return s.Substring(0, i);
            }
            return s;
        }
    }
}
