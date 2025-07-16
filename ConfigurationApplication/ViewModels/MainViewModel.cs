using ConfigurationApplication.Commands;
using ConfigurationApplication.Helpers;
using ConfigurationApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        public ICommand PingCommand { get; }
        public ICommand ReadEthernetCommand { get; } 
        public ICommand ResetCommand { get; }
        public ICommand GetMACCommand { get; }

        public MainViewModel()
        {
            SendRS485Command = new RelayCommand(SendRS485);
            ReadRS485Command = new RelayCommand(ReadRS485);
            SendEthernetCommand = new RelayCommand(SendEthernet);
            PingCommand = new RelayCommand(PingDevice);
            ReadEthernetCommand = new RelayCommand(ReadEthernet);
            GetMACCommand = new RelayCommand(GetMAC);
            ResetCommand = new RelayCommand(ResetDevice);
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
            if (!_tcpHelper.Connect())
            {
                MessageBox.Show("Connection failed.", "TCP Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] config = new byte[88];
            int offset = 0;

            // Write is_static as 4-byte int
            BitConverter.GetBytes(Ethernet.IsStatic ? 1 : 0).CopyTo(config, offset);
            offset += 4;

            // Helper to copy string
            void CopyString(string value, int length)
            {
                Encoding.ASCII.GetBytes((value ?? "").PadRight(length, '\0')).CopyTo(config, offset);
                offset += length;
            }

            CopyString(Ethernet.IP, 16);
            CopyString(Ethernet.Gateway, 16);
            CopyString(Ethernet.Netmask, 16);
            CopyString(Ethernet.DnsMain, 16);
            CopyString(Ethernet.DnsBackup, 16);

            // Write port as 4-byte int (not ushort)
            BitConverter.GetBytes(Ethernet.Port).CopyTo(config, offset); // offset now at 84
                                                                         // No increment needed, done

            _tcpHelper.SendCommand("SETNW", config);
            _tcpHelper.Disconnect();

            MessageBox.Show("Ethernet configuration sent.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void PingDevice()
        {
            if (string.IsNullOrWhiteSpace(Ethernet.IP))
            {
                MessageBox.Show("Please enter a valid IP address.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ping = new Ping();
                var reply = ping.Send(Ethernet.IP, 1000); // 1 sec timeout

                if (reply.Status == IPStatus.Success && reply.RoundtripTime > 0)
                {
                    MessageBox.Show($"Ping successful!\nTime: {reply.RoundtripTime} ms", "Ping Result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ping failed: No response from device.", "Ping Result", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ping error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadEthernet()
        {
            if (!_tcpHelper.Connect())
            {
                MessageBox.Show("Connection failed.", "TCP Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _tcpHelper.SendCommand("GETNW");
            var buffer = _tcpHelper.ReadResponse(88);

            if (buffer.Length < 88)
            {
                MessageBox.Show("Response too short.", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int offset = 0;

            Ethernet.IsStatic = BitConverter.ToInt32(buffer, offset) == 1;
            offset += 4;

            string ReadString(int length)
            {
                string result = Encoding.ASCII.GetString(buffer, offset, length).Trim('\0');
                offset += length;
                return result;
            }

            Ethernet.IP = ReadString(16);
            Ethernet.Gateway = ReadString(16);
            Ethernet.Netmask = ReadString(16);
            Ethernet.DnsMain = ReadString(16);
            Ethernet.DnsBackup = ReadString(16);

            Ethernet.Port = (ushort)BitConverter.ToInt32(buffer, offset); // 4 bytes int

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

        private void ResetDevice()
        {
            if (!_tcpHelper.Connect())
            {
                MessageBox.Show("Could not connect to device.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _tcpHelper.SendCommand("RST");

            _tcpHelper.Disconnect();

            MessageBox.Show("Device reset success", "Reset Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }


#pragma warning disable CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
        private void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
