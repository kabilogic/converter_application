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
        public ICommand GetMACCommand { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public MainViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            SendRS485Command = new RelayCommand(SendRS485);
            ReadRS485Command = new RelayCommand(ReadRS485);
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
