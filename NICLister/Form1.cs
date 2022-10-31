using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NICLister
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.ColumnCount = 7;
            dataGridView1.Columns[0].Name = "NIC";
            dataGridView1.Columns[1].Name = "Name";
            dataGridView1.Columns[2].Name = "MAC";
            dataGridView1.Columns[3].Name = "IPv4";
            dataGridView1.Columns[4].Name = "IPv6";
            dataGridView1.Columns[5].Name = "DNSv4";
            dataGridView1.Columns[6].Name = "DNSv6";

            string[] ExcludeIFList = { "Wintun", "TAP", "Loopback", "Microsoft Wi-Fi", "VMware" };
            bool ContainsIF = false;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                foreach (string x in ExcludeIFList)
                {
                    if (adapter.Description.Contains(x))
                    {
                        ContainsIF = true;
                    }
                }
                if (!ContainsIF)
                {
                    var ipv4 = new List<string>();
                    var ipv6 = new List<string>();
                    var dnsv4 = new List<string>();
                    var dnsv6 = new List<string>();
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (IPtype(unicast.Address.ToString()) == 6)
                        {
                            if (!(IsIPv6LocalLink(unicast.Address.ToString())))
                                ipv6.Add(unicast.Address.ToString());
                        }
                        else if (IPtype(unicast.Address.ToString()) == 4)
                        {
                            ipv4.Add(unicast.Address.ToString());
                        }
                    }

                    IPAddressCollection dnsServers = properties.DnsAddresses;
                    if (dnsServers.Count > 0)
                    {
                        foreach (IPAddress d in dnsServers)
                        {
                            if (IPtype(d.ToString()) == 4)
                            {
                                dnsv4.Add(d.ToString());
                            }
                            else if (IPtype(d.ToString()) == 6)
                            {
                                dnsv6.Add(d.ToString());
                            }
                        }
                    }

                    var ipv4list = String.Join(", ", ipv4.ToArray());
                    var ipv6list = String.Join(", ", ipv6.ToArray());
                    var dnsv4List = String.Join("\r\n", dnsv4.ToArray()); 
                    var dnsv6List = String.Join("\r\n", dnsv6.ToArray());

                    dataGridView1.Rows.Add(adapter.Description, adapter.Name, FormatMAC(System.Convert.ToString(adapter.GetPhysicalAddress())), ipv4list, ipv6list, dnsv4List, dnsv6List);
                }
                ContainsIF = false;
            }
            dataGridView1.AutoResizeColumns();
            dataGridView1.AutoResizeRows();
        }

        public bool IsIPv6LocalLink(string input)
        {
            Regex rx = new Regex(@"fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}$");
            if (rx.IsMatch(input))
                return true;
            else
                return false;
        }

        public int IPtype(string input)
        {
            IPAddress address;
            if (IPAddress.TryParse(input, out address))
            {
                switch(address.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        //we have IPv4
                        return 4;
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        //we have IPv6
                        return 6;
                    default:
                        //PUH
                        return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public string FormatMAC(string input)
        {
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";
            var newformat = Regex.Replace(input, regex, replace);
            return newformat;
        }
    }
}
