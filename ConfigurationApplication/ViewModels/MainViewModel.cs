using ConfigurationApplication.Commands;
using ConfigurationApplication.Helpers;
using ConfigurationApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConfigurationApplication.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly TcpClientHelper _tcpHelper = new();

        public RS485Settings RS485 { get; set; } = new();
        public EthernetSettings Ethernet { get; set; } = new();

        private string _macAddress;
        public string MACAddress
        {
            get => _macAddress;
            set
            {
                _macAddress = value;
                OnPropertyChanged();
            }
        }

        public ICommand SendRS485Command { get; }
        public ICommand ReadRS485Command { get; }
        public ICommand SendEthernetCommand {  get; }
        public ICommand ReadEthernetCommand { get; } 
        public ICommand GetMACCommand { get; }

        public MainViewModel()
        {
            SendRS485Command = new RelayCommand(SendRS485);
            ReadRS485Command = new RelayCommand(ReadRS485);
            SendEthernetCommand = new RelayCommand(SendEthernet);
            ReadEthernetCommand = new RelayCommand(ReadEthernet);
            GetMACCommand = new RelayCommand(GetMAC);
        }

        private void SendRS485()
        {
            if (!_tcpHelper.Connect()) return;

            byte[] config = new byte[7];
            Buffer.BlockCopy(BitConverter.GetBytes(RS485.BaudRate), 0, config, 0, 4);
            config[4] = RS485.Parity;
            config[5] = RS485.DataBit;
            config[6] = RS485.StopBit;

            _tcpHelper.SendCommand("SETRS485", config);
            _tcpHelper.Disconnect();
        }

        private void ReadRS485()
        {
            if (!_tcpHelper.Connect()) return;

            _tcpHelper.SendCommand("GETRS485");
            var data = _tcpHelper.ReadResponse(7);

            RS485.BaudRate = BitConverter.ToUInt32(data, 0);
            RS485.Parity = data[4];
            RS485.DataBit = data[5];
            RS485.StopBit = data[6];

            OnPropertyChanged(nameof(RS485));
            _tcpHelper.Disconnect();
        }

        private void SendEthernet()
        {
            if(_tcpHelper.Connect()) return;

            byte[] config = new byte[81];

            config[0] = Ethernet.IsStatic ? (byte)1 : (byte)0;
            Encoding.ASCII.GetBytes(Ethernet.IP.PadRight(16, '\0')).CopyTo(config, 1);
            Encoding.ASCII.GetBytes(Ethernet.Gateway.PadRight(16, '\0')).CopyTo(config, 17);
            Encoding.ASCII.GetBytes(Ethernet.Netmask.PadRight(16, '\0')).CopyTo(config, 33);
            Encoding.ASCII.GetBytes(Ethernet.DnsMain.PadRight(16, '\0')).CopyTo(config, 49);
            Encoding.ASCII.GetBytes(Ethernet.DnsBackup.PadRight(16, '\0')).CopyTo(config, 65);
            BitConverter.GetBytes(Ethernet.Port).CopyTo(config, 81 - 2);

            _tcpHelper.SendCommand("SETNW", config);
            _tcpHelper.Disconnect();
        }

        private void ReadEthernet()
        {
            if (!_tcpHelper.Connect()) return;

            _tcpHelper.SendCommand("GETNW");
            var buffer = _tcpHelper.ReadResponse(81);

            Ethernet.IsStatic = buffer[0] == 1;
            Ethernet.IP = Encoding.ASCII.GetString(buffer, 1, 16).Trim('\0');
            Ethernet.Gateway = Encoding.ASCII.GetString(buffer, 17, 16).Trim('\0');
            Ethernet.Netmask = Encoding.ASCII.GetString(buffer, 33, 16).Trim('\0');
            Ethernet.DnsMain = Encoding.ASCII.GetString(buffer, 49, 16).Trim('\0');
            Ethernet.DnsBackup = Encoding.ASCII.GetString(buffer, 65, 16).Trim('\0');
            Ethernet.Port = BitConverter.ToUInt16(buffer, 79);

            OnPropertyChanged(nameof(Ethernet));
            _tcpHelper.Disconnect();
        }
        private void GetMAC()
        {
            if (!_tcpHelper.Connect()) return;

            _tcpHelper.SendCommand("GETMAC");
            var mac = _tcpHelper.ReadResponse(6);
            MACAddress = string.Join(":", mac.Select(b => b.ToString("X2")));
            _tcpHelper.Disconnect();
        }

#pragma warning disable CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
        private void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
