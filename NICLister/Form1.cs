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
            this.KeyPreview = true;
        }
        class Global
        {
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            GetInterfaces();
        }
        public void GetInterfaces()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            dataGridView1.ColumnCount = 8;
            dataGridView1.Columns[0].Name = "Description";
            dataGridView1.Columns[1].Name = "Name";
            dataGridView1.Columns[2].Name = "MAC";
            dataGridView1.Columns[3].Name = "IPv4";
            dataGridView1.Columns[4].Name = "DNS";
            dataGridView1.Columns[5].Name = "Gateway";
            dataGridView1.Columns[6].Name = "DHCP Active";
            dataGridView1.Columns[7].Name = "DHCP-Server";


            string[] ExcludeIFList = { "Wintun", "TAP", "Loopback", "Microsoft Wi-Fi", "VMware", "Bluetooth"};
            bool ContainsIF = false;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters) // Schleife die jeden einzelnen Adapter im System abarbeitet
            {
                foreach (string x in ExcludeIFList) // Interfaces filtern
                {
                    if (adapter.Description.Contains(x))
                    {
                        ContainsIF = true;
                    }
                }
                if (!ContainsIF) // wenn Interface kein gefiltertes Interface ist
                {
                    // Listen fuer Erfassung der Adressen initialisieren
                    var ipv4 = new List<string>();
                    var ipv6 = new List<string>();
                    var dnsv4 = new List<string>();
                    var dnsv6 = new List<string>();
                    var gwv4 = new List<string>();
                    var gwv6 = new List<string>();
                    var dhcpv4 = new List<string>();
                    var dhcpv6 = new List<string>();
                    string dhcpenabled;

                    // Check if IF is DHCP-enabled
                    if (adapter.GetIPProperties().GetIPv4Properties().IsDhcpEnabled)
                    {
                        dhcpenabled = "YES";
                    }
                    else
                    {
                        dhcpenabled = "NO";
                    }

                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    // IP-Adressen (v4 und v6)
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

                    // DNS-Server (v4 und v6)
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

                    // DHCP-Server (v4 und v6)
                    IPAddressCollection dhcpserver = properties.DhcpServerAddresses;
                    if (dhcpserver.Count > 0)
                    {
                        foreach (IPAddress d in dhcpserver)
                        {
                            if (IPtype(d.ToString()) == 6)
                            {
                                dhcpv6.Add(d.ToString());
                            }
                            else if (IPtype(d.ToString()) == 4)
                            {
                                dhcpv4.Add(d.ToString());
                            }
                        }
                    }

                    // Standardgateway (v4 und v6)
                    GatewayIPAddressInformationCollection defaultGW = properties.GatewayAddresses;
                    if (defaultGW.Count > 0)
                    {
                        foreach (GatewayIPAddressInformation address in defaultGW)
                        {
                            if (IPtype(address.Address.ToString()) == 6)
                            {
                                if (!(IsIPv6LocalLink(address.Address.ToString()))) 
                                    gwv6.Add(address.Address.ToString());
                            }
                            else if (IPtype(address.Address.ToString()) == 4)
                            {
                                gwv4.Add(address.Address.ToString());
                            }
                        }
                    }

                    // v4 mit v6 - Listen kombinineren
                    ipv4.AddRange(ipv6);
                    dnsv4.AddRange(dnsv6);
                    gwv4.AddRange(gwv6);
                    dhcpv4.AddRange(dhcpv6);

                    // Listen formatieren (CR/LF)
                    var iplist = String.Join("\r\n", ipv4.ToArray());
                    var dnslist = String.Join("\r\n", dnsv4.ToArray());
                    var gwlist = String.Join("\r\n", gwv4.ToArray());
                    var dhcplist = String.Join("\r\n", dhcpv4.ToArray());

                    // Daten im DataGridView ausgeben
                    dataGridView1.Rows.Add(adapter.Description, adapter.Name, FormatMAC(System.Convert.ToString(adapter.GetPhysicalAddress())), iplist, dnslist, gwlist, dhcpenabled, dhcplist);
                }
                ContainsIF = false;
            }
            dataGridView1.AutoResizeColumns();
            dataGridView1.AutoResizeRows();
            //this.Width = dataGridView1.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) + 35;
            //this.Height = dataGridView1.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + 110;
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

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F5)
            {
                GetInterfaces();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("NCPA.cpl");
        }
    }
}