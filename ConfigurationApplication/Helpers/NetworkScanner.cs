using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationApplication.Helpers
{
    public static class NetworkScanner
    {
        public static async Task<List<string>> ScanSubnetAsync(string subnetBase, int port = 1502, int timeout = 200)
        {
            var reachableIPs = new List<string>();
            var tasks = Enumerable.Range(1, 254).Select(async i =>
            {
                string ip = $"{subnetBase}.{i}";
                using var client = new TcpClient();

                try
                {
                    var connectTask = client.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(timeout);

                    if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && client.Connected)
                    {
                        lock (reachableIPs) reachableIPs.Add(ip);
                    }
                }
                catch
                {

                }
            });

            await Task.WhenAll(tasks);
            return reachableIPs;
        }
    }
}
