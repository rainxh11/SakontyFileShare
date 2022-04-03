using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using EmbedIO;
using Akavache;
using System.Reactive;
using System.Threading;
using EmbedIO.Actions;
using System.Reactive.Linq;
using Refit;
using DynamicData;

namespace SakontyFileShare
{
    public partial class SakontyNode : ISakontyNode
    {
        private SourceCache<ISakontyPeer, string> _peerList = new SourceCache<ISakontyPeer, string>(x => x.Hwid);
        public bool IsListening => _server == null ? true : _server.Listener.IsListening;
        private CancellationToken _serverCt;
        private CancellationToken _scanCt;
        private CancellationTokenSource _scanCancellationToken;
        private IPeerApi _peerApi;
        private IPEndPoint _endPoint;
#nullable enable
        private WebServer? _server;
        private IDisposable? _scanObservable;
        private IDisposable? _identityRefreshObservable;
#nullable disable
        private SakontyNode(CancellationToken ct, WebServer server, IPEndPoint endpoint)
        {
            _server = server;
            _serverCt = ct;
            _server.Start(_serverCt);
            _scanCancellationToken = new CancellationTokenSource();
            _scanCt = _scanCancellationToken.Token;
            _endPoint = endpoint;
        }
        private void StopScanning()
        {
            _scanCancellationToken.Cancel();
            _scanCancellationToken = new CancellationTokenSource();
            _scanCt = _scanCancellationToken.Token;
        }
        private void StopWatchers()
        {
            _scanObservable?.Dispose();
        }
        private void StartWatchers()
        {
            _scanObservable = Observable
                .Interval(TimeSpan.FromSeconds(5))
                .Repeat()
                .CombineLatest(ScanForPeers(_endPoint.Address.ToString(), _endPoint.Port))
                .Select(x => x.Second)
                .Select(x => CreatePeer(x))
                .Do(async peer =>
                {
                    try
                    {
                        await BlobCache.InMemory.InsertObject<ISakontyPeer>(peer.Hwid, peer);
                    }
                    catch
                    {

                    }
                })
                .Subscribe();

            _identityRefreshObservable = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Repeat()
                .CombineLatest(BlobCache.InMemory.GetAllObjects<ISakontyPeer>())
                .Select(x => x.Second)
                .Do(peers => _peerList.Edit(x => x.AddOrUpdate(peers)))
                .Subscribe();
        }
        public ISakontyPeer GetUnderlyingPeer()
        {
            IsSeed = true;
            return this;
        }
        private async Task CreateIdentiy()
        {
            var identity = new PeerIdentity()
            {
                Address = _endPoint.Address.ToString(),
                Port = _endPoint.Port,
                Hwid = GetHardwardID(),
                MachineName = GetMachineName(),
                Name = Environment.UserName
            };
            await BlobCache.InMemory.InsertObject("identity", identity);
        }
        private SakontyNode()
        {

        }
        public static INodeCreator CreateNode()
        {
            return new SakontyNode();
        }
        public async Task<ISakontyNode> StartSeeding(CancellationToken cancellationToken, IPAddress address, int port = 5050)
        {
            if (!IsListening)
            {
                var server = new WebServer(options =>
                options
                    .WithUrlPrefix($"http://{address}:{port}")
                    .WithMode(HttpListenerMode.EmbedIO))
                    .WithWebApi("/peer", api => api.WithController<PeerController>())
                    .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));
                var endPoint = new IPEndPoint(address, port);

                await CreateIdentiy();
                return new SakontyNode(cancellationToken, server, endPoint);
            }
            else
            {
                throw new Exception("Seed is already listening.");
            }
        }

        public async Task<ISakontyNode> StartSeeding(CancellationToken cancellationToken, string bindIp = "0.0.0.0.", int port = 5050)
        {
            if (!IsListening)
            {
                var server = new WebServer(options =>
                options
                    .WithUrlPrefix($"http://{bindIp}:{port}")
                    .WithMode(HttpListenerMode.EmbedIO))
                    .WithWebApi("/peer", api => api.WithController<PeerController>())
                    .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

                var endPoint = new IPEndPoint(IPAddress.Parse(bindIp), port);
                await CreateIdentiy();

                return new SakontyNode(cancellationToken, server, endPoint);
            }
            else
            {
                throw new Exception("Seed is already listening.");
            }
        }

        public void StopSeeding()
        {
            if (this.IsListening)
                _server.Dispose();
        }

        public async Task<IPeerFile> AddShareFolder(DirectoryInfo folder)
        {
            var localShare = new LocalShareFolder(folder);

            await BlobCache.LocalMachine.InsertObject(localShare.Name , localShare);
            return localShare;
        }
        public async Task<IPeerFile> AddFile(FileInfo file, bool calculateCheckSum = true)
        {
            var localFile = new LocalFile(file, this.GetUnderlyingIdentity());
            if (calculateCheckSum)
            {
                localFile.Checksum = await CreateChecksum(file);
            }
            await BlobCache.LocalMachine.InsertObject(localFile.Id, localFile);
            return localFile.ToPeerFile();
        }

        public async Task<IPeerFile> AddFile(FileInfo file, TimeSpan expiration, bool calculateCheckSum = true)
        {
            var localFile = new LocalFile(file, this.GetUnderlyingIdentity());
            if (calculateCheckSum)
            {
                localFile.Checksum = await CreateChecksum(file);
            }
            await BlobCache.LocalMachine.InsertObject(localFile.Id, localFile, expiration);
            return localFile.ToPeerFile();
        }

        public async Task<IPeerFile> AddFile(FileInfo file, DateTime expirationDate, bool calculateCheckSum = true)
        {
            var localFile = new LocalFile(file, this.GetUnderlyingIdentity());
            if (calculateCheckSum)
            {
                localFile.Checksum = await CreateChecksum(file);
            }
            await BlobCache.LocalMachine.InsertObject(localFile.Id, localFile, expirationDate);
            return localFile.ToPeerFile();
        }

        public async Task<bool> BlockPeer(params IPeerIdentity[] peers)
        {
            var blockedPeers = peers
                .Select(peer => peer.SetBlockFlag(true))
                .ToDictionary(x => x.Hwid);
            try
            {
                await BlobCache.LocalMachine.InsertAllObjects(blockedPeers);
            }
            catch
            {
                return false;
            }
            return true;

        }

        public async Task<bool> BlockPeer(TimeSpan duration, params IPeerIdentity[] peers)
        {
            var blockedPeers = peers
                .Select(peer => peer.SetBlockFlag(true))
                .ToDictionary(x => x.Hwid);
            try
            {
                await BlobCache.LocalMachine.InsertAllObjects(blockedPeers, DateTime.Now.Add(duration));
            }
            catch
            {
                return false;
            }
            return true;
        }

        public async Task<bool> BlockPeer(DateTime unblockDate, params IPeerIdentity[] peers)
        {
            var blockedPeers = peers
                .Select(peer => peer.SetBlockFlag(true))
                .ToDictionary(x => x.Hwid);
            try
            {
                await BlobCache.LocalMachine.InsertAllObjects(blockedPeers, unblockDate);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public async Task ClearCache()
        {
            await BlobCache.LocalMachine.InvalidateAll();
            await BlobCache.InMemory.InvalidateAll();
            await CreateIdentiy();
        }

        public async IAsyncEnumerable<ISakontyPeer> ConnectPeers(params IPeerIdentity[] peers)
        {
            var ipList = peers.ToDictionary(x => x.Address, x => x.Port).ToArray();
            await foreach(var ip in SkNetwork.PortScan(_scanCt, ipList))
            {
                ISakontyPeer peer = null;
                try
                {
                    var api = RestService.For<IPeerApi>($"http://{ip.ToString()}");
                    var identity = await api.Echo();
                    peer = CreatePeer(identity);
                    await BlobCache.LocalMachine.InsertObject(peer.Hwid, peer);
                }
                catch
                {

                }
                yield return peer;
            }
        }

        public async IAsyncEnumerable<ISakontyPeer> ConnectPeers(params KeyValuePair<string, int>[] addresses)
        {
            await foreach (var ip in SkNetwork.PortScan(_scanCt, addresses))
            {
                ISakontyPeer peer = null;
                try
                {
                    var api = RestService.For<IPeerApi>($"http://{ip.ToString()}");
                    var identity = await api.Echo();
                    peer = CreatePeer(identity);
                    await BlobCache.LocalMachine.InsertObject(peer.Hwid, peer);
                }
                catch
                {

                }
                yield return peer;
            }
        }

        public async IAsyncEnumerable<ISakontyPeer> ConnectPeers(params IPEndPoint[] endpoints)
        {
            await foreach (var ip in SkNetwork.PortScan(_scanCt, endpoints))
            {
                ISakontyPeer peer = null;
                try
                {
                    var api = RestService.For<IPeerApi>($"http://{ip.ToString()}");
                    var identity = await api.Echo();
                    peer = CreatePeer(identity);
                    await BlobCache.LocalMachine.InsertObject(peer.Hwid, peer);
                }
                catch
                {

                }
                yield return peer;
            }
        }
        public IEnumerable<ISakontyPeer> GetPeers()
        {
            return _peerList.Items;
        }

        public async Task<bool> RemoveFile(string fileId)
        {
            try
            {
                await BlobCache.LocalMachine.InvalidateObject<ILocalFile>(fileId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IPeerIdentity> SetIdentityName(string name)
        {
            this.Name = name;
            await BlobCache.InMemory.InsertObject("identity", GetUnderlyingIdentity());
            return this;
        }

        public async Task<bool> UnblockPeer(params IPeerIdentity[] peers)
        {
            var blockedPeers = peers
                .Select(peer => peer.SetBlockFlag(false))
                .ToDictionary(x => x.Hwid);
            try
            {
                await BlobCache.LocalMachine.InsertAllObjects(blockedPeers);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public async Task<IPeerFile> UpdateFileExpiration(string fileId, DateTime expirationDate)
        {
            var file = await BlobCache.LocalMachine.GetObject<ILocalFile>(fileId);
            await BlobCache.LocalMachine.InsertObject(fileId, file, expirationDate);
            return file;
        }
    }
}
