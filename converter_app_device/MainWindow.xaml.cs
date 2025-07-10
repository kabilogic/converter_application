using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace converter_app_device;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string IP = "192.168.0.130";
    private const int PORT = 1502;

    public MainWindow()
    {
        InitializeComponent();
    }

    private TcpClient ConnectToDevice()
    {
        var client = new TcpClient();
        client.Connect(IP, PORT);
        return client;
    }

    private void SendRS485_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var client = ConnectToDevice();
            using var stream = client.GetStream();

            byte[] config = new byte[7];
            uint baudRate = uint.Parse(BaudRateBox.Text);
            config[0] = (byte)(baudRate & 0xFF);
            config[1] = (byte)((baudRate >> 8) & 0xFF);
            config[2] = (byte)((baudRate >> 16) & 0xFF);
            config[3] = (byte)((baudRate >> 24) & 0xFF);
            config[4] = byte.Parse(ParityBox.Text);
            config[5] = byte.Parse(DataBitBox.Text);
            config[6] = byte.Parse(StopBitBox.Text);

            var cmd = Encoding.ASCII.GetBytes("SETRS485");
            var payload = cmd.Concat(config).ToArray();
            stream.Write(payload, 0, payload.Length);
            MessageBox.Show("RS485 settings sent.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message);
        }
    }

    private void ReadRS485_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var client = ConnectToDevice();
            using var stream = client.GetStream();

            var cmd = Encoding.ASCII.GetBytes("GETRS485");
            stream.Write(cmd, 0, cmd.Length);

            byte[] buffer = new byte[7];
            stream.Read(buffer, 0, buffer.Length);

            uint baudRate = BitConverter.ToUInt32(buffer, 0);
            byte parity = buffer[4];
            byte dataBit = buffer[5];
            byte stopBit = buffer[6];

            BaudRateBox.Text = baudRate.ToString();
            ParityBox.Text = parity.ToString();
            DataBitBox.Text = dataBit.ToString();
            StopBitBox.Text = stopBit.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message);
        }
    }

    private void GetMAC_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var client = ConnectToDevice();
            using var stream = client.GetStream();

            var cmd = Encoding.ASCII.GetBytes("GETMAC");
            stream.Write(cmd, 0, cmd.Length);

            byte[] mac = new byte[6];
            stream.Read(mac, 0, mac.Length);
            string macStr = string.Join(":", mac.Select(b => b.ToString("X2")));
            MacAddressText.Text = macStr;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message);
        }
    }
}