using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Blake3;
using DeviceId;
using Refit;

namespace SakontyFileShare
{
    public class ChecksumHelper
    {
        public static async Task<bool> CheckFile(IPeerFile peerFile, FileInfo file)
        {
            if(peerFile.Checksum == null)
            {
                return true;
            }
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
            {
                using (var hashStream = new Blake3Stream(fs))
                {
                    await hashStream.ReadAsync(new byte[file.Length]);
                    var hash = hashStream.ComputeHash();
                    return hash.ToString() == peerFile.Checksum;
                }
            }
        }

        public static async Task<string> CreateChecksum(FileInfo file)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
            {
                using (var hashStream = new Blake3Stream(fs))
                {
                    await hashStream.ReadAsync(new byte[file.Length]);
                    var hash = hashStream.ComputeHash();
                    return hash.ToString();
                }
            }
        }
    }
    public partial class SakontyNode : ISakontyNode
    {
        public async Task<bool> CheckFile(IPeerFile peerFile, FileInfo file)
        {
            return await ChecksumHelper.CheckFile(peerFile, file);
        }

        public async Task<string> CreateChecksum(FileInfo file)
        {
            return await ChecksumHelper.CreateChecksum(file);
        }

        public string GetHardwardID()
        {
            return new DeviceIdBuilder()
               .AddProcessorId()
               .AddProcessorName()
               .AddMotherboardInfo("Manufacturer")
               .AddMotherboardInfo("Product")
               .AddMotherboardInfo("Version")
               .AddMacAddress(true, true)
               //.AddOSInstallationID()
               //.AddOSVersion()
               //.AddUserName()
               //.AddSystemUUID()
               .ToString();
        }

        public string GetMachineName()
        {
            return $"{Environment.MachineName}_{Environment.UserName}";
        }

        public IObservable<IPeerIdentity> ScanForPeers(string ip, int port = 5050)
        {
            return Observable.Create<IPeerIdentity>(async observer =>
            {
                try
                {
                    var ipAddress = IPAddress.Parse(ip);
                    var subnet = await SkNetwork.GetSubnetMask(ipAddress);

                    await foreach (var resultIp in SkNetwork.PortScan(port, _serverCt))
                    {
                        var api = RestService.For<IPeerApi>($"http://{resultIp}:{port}");

                        var identity = await api.Echo();
                        observer.OnNext(identity);
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });
        }
    }
}
