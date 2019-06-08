using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LanChat
{
    public partial class SelectAdapterForm : Form
    {
        public List<AdapterListInfo> list;
        public SelectAdapterForm()
        {
            InitializeComponent();
            scanAdapter();
            comboBox1.DataSource = list;
            comboBox1.ValueMember = "value";
            comboBox1.DisplayMember = "key";

        }
        public void scanAdapter()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            list = new List<AdapterListInfo>();
            foreach (NetworkInterface adapter in adapters)
            {
                if(adapter.NetworkInterfaceType!=NetworkInterfaceType.Loopback)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();//获取IP配置
                    UnicastIPAddressInformationCollection ipCollection = ipProperties.UnicastAddresses;//获取单播地址集
                    
                    GatewayIPAddressInformationCollection gwCollection = ipProperties.GatewayAddresses;
                    foreach (UnicastIPAddressInformation ip in ipCollection)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            
                            Console.WriteLine("name:" + adapter.Name);
                            Console.WriteLine("type:" + adapter.NetworkInterfaceType);
                            Console.WriteLine("ip:" + ip.Address);
                            Console.WriteLine("ipSubnet::" + ip.IPv4Mask);
                            Console.WriteLine("mac:"+adapter.GetPhysicalAddress());
                            Console.WriteLine();
                            list.Add(new AdapterListInfo() {
                                key =adapter.Name+"---"+ip.Address,
                                value =new Adapter(adapter.Name, adapter.NetworkInterfaceType, ip.Address, ip.IPv4Mask,adapter.GetPhysicalAddress())
                            });
                        }
                    }
                }
                
            }
        }

        private void confrim_Click(object sender, EventArgs e)
        {
            Adapter adpter = ((AdapterListInfo)comboBox1.SelectedItem).value;
            Form1 form1 = new Form1(adpter);
            this.Hide();
            form1.Show();
        }

        private void SelectAdapterForm_Load(object sender, EventArgs e)
        {

        }
    }
    public class AdapterListInfo
    {
        public string key { get; set; }
        public Adapter value { get; set; }
    }
    public class Adapter
    {
        public Adapter(string Name,NetworkInterfaceType type,IPAddress ip,IPAddress ipSubnet,PhysicalAddress mac)
        {
            this.Name = Name;
            this.type = type;
            this.ip = ip;
            this.ipSubnet = ipSubnet;
            this.mac = mac;
        }
        public string Name;
        public NetworkInterfaceType type;
        public IPAddress ip;
        public IPAddress ipSubnet;
        public PhysicalAddress mac;
    }
}
