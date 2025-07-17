using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationApplication.Helpers
{
    public  class TcpClientHelper
    {
        //private const string IP_ADDRESS = "192.168.0.120";
        private string _ipAddress;
        private int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public TcpClientHelper(string ipAddress, int port = 1502)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public bool Connect(string ip, int port = 1502)
        { 
            try
            {
                _client = new TcpClient();
                _client.Connect(_ipAddress, _port); 
                _stream = _client.GetStream();
                return true;
            }
            catch 
            { 
                return false; 
            }
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }

        public void SendCommand(string cmd, byte[]? data = null)
        {
            if (_stream == null) throw new InvalidOperationException("Not connected to TCP stream.");

            var cmdBytes = Encoding.ASCII.GetBytes(cmd);
            var payload = data == null ? cmdBytes : cmdBytes.Concat(data).ToArray();
            _stream.Write(payload, 0, payload.Length);
        }

        public byte[] ReadResponse(int length)
        {
            if (_stream == null) throw new InvalidOperationException("Not connected to TCP stream.");

            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int read = _stream.Read(buffer, totalRead, length - totalRead);
                if (read == 0)
                    throw new Exception("Connection closed before all data was received.");

                totalRead += read;
            }

            return buffer;
        }
    }
}
