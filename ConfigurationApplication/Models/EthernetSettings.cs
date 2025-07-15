using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationApplication.Models
{
    public class EthernetSettings
    {
        public bool IsStatic { get; set; }
        public string IP { get; set; } = "";
        public string Gateway { get; set; } = "";
        public string Netmask { get; set; } = "";
        public string DnsMain { get; set; } = "";
        public string DnsBackup { get; set; } = "";
        public ushort Port { get; set; }
    }
}
