using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Runtime.CompilerServices;

namespace SakontyFileShare
{
    public class SkNetwork
    {
        public static Task<IPAddress> GetSubnetMask(IPAddress address)
        {
            try 
            {
                UnicastIPAddressInformation returnIp = null;


                foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {

                    foreach (var unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (address.Equals(unicastIPAddressInformation.Address))
                            {
                                returnIp = unicastIPAddressInformation;
                            }
                        }
                    }
                }
                return Task.FromResult<IPAddress>(returnIp.IPv4Mask);
            }
            catch
            {
                throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
            }           
        }
        public static IEnumerable<IPAddress> GetAddressFromSubnet(IPAddress address, IPAddress netmask)
        {
            if(IPNetwork.TryParse(address, netmask, out var ipNetwork))
            {
                return ipNetwork.ListIPAddress().AsEnumerable();
            }
            else
            {
                return Enumerable.Empty<IPAddress>();
            }
        }
        public static async IAsyncEnumerable<IPEndPoint> PortScan([EnumeratorCancellation] CancellationToken ct, params IPEndPoint[] ipList)
        {

            foreach (var ip in ipList)
            {
                var connected = await Task.Run(() =>
                {
                    using (TcpClient scan = new TcpClient())
                    {
                        try
                        {
                            scan.Connect(ip.Address, ip.Port);
                            return scan.Connected;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }, ct);
                if (connected)
                    yield return ip;
            }
        }
        public static async IAsyncEnumerable<IPEndPoint> PortScan([EnumeratorCancellation] CancellationToken ct, params KeyValuePair<string, int>[] ipList)
        {

            foreach (var ip in ipList)
            {
                var connected = await Task.Run(() =>
                {
                    using (TcpClient scan = new TcpClient())
                    {
                        try
                        {
                            scan.Connect(ip.Key, ip.Value);
                            return scan.Connected;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }, ct);
                if (connected)
                    yield return new IPEndPoint(IPAddress.Parse(ip.Key), ip.Value);
            }
        }

        public static async IAsyncEnumerable<IPAddress> PortScan(int port, [EnumeratorCancellation] CancellationToken ct, params IPAddress[] ipList)
        {

            foreach(var ip in ipList)
            {
                var connected = await Task.Run(() =>
                {
                    using (TcpClient scan = new TcpClient())
                    {
                        try
                        {
                            scan.Connect(ip, port);
                            return scan.Connected;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }, ct);
                if (connected)
                    yield return ip;
            }
        }
    }
}
