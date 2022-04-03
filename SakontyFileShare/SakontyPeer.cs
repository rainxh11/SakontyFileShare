using Jetsons.JetPack;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SakontyFileShare
{
    public partial class SakontyNode : ISakontyNode
    {
        public string Name { get; set; }
        public string MachineName { get; set; }
        public string Hwid { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public bool Blocked { get; set; }
        public IPeerIdentity SetBlockFlag(bool flag)
        {
            Blocked = flag;
            return this;
        }
        public bool IsSeed { get; private set; }
        public bool IsConnected => throw new NotImplementedException();

        private SakontyNode(IPeerApi peerApi)
        {
            _peerApi = peerApi;
        }

        public ISakontyPeer CreatePeer(IPeerIdentity peerIdentity)
        {
            var api = RestService.For<IPeerApi>($"http://{peerIdentity.Address}:{peerIdentity.Port}");
            return new SakontyNode(api)
            {
                Address = peerIdentity.Address,
                MachineName = peerIdentity.MachineName,
                Hwid = peerIdentity.Hwid,
                Port = peerIdentity.Port,
            };
        }

        public void Connect()
        {
            ConnectPeers(GetUnderlyingIdentity()).ConfigureAwait(false);
        }

        public async Task<IObservable<float>> DownloadFile(IPeerFile file, string destination, CancellationToken ct)
        {
            var clientApi = RestService.For<IPeerApi>($"http://{file.CreatedBy.Address}:{file.CreatedBy.Port}");
            var response = await clientApi.DownloadFile(file.Id);
            var httpStream = response.Content.ReadAsStream();
            var contentLength = long.Parse(response.Headers.First(x => x.Key == "Content-Length").Value.First());

            var fileStream = new FileStream(destination, FileMode.Create);

            return ObservableProgress
                .CreateAsync<float>(reporter => httpStream.CopyToAsync(fileStream, 1024 * 1024, reporter, contentLength, _serverCt));
        }

        public async Task<IEnumerable<IPeerFile>> GetFiles()
        {
            var api = RestService.For<IPeerApi>($"http://{Address}:{Port}");

            return await api.GetFiles();
        }

        public IPeerApi GetUnderlyingApi()
        {
            return RestService.For<IPeerApi>($"http://{Address}:{Port}");
        }
        public IPeerIdentity GetUnderlyingIdentity()
        {
            return this;
        }
        public Task<Stream> StreamFile(IPeerFile file, IProgress<float> progress, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> StreamFile(IPeerFile file, long startPosition, int bufferLength, IProgress<float> progress, CancellationToken ct)
        {
            var api = GetUnderlyingApi();
            var response = await api.DownloadFile(file.Id);
            var length = response.Headers.First(x => x.Key == "Content-Length").Value.First().ToInt(0);
            var outputStream = new MemoryStream();
            var inputStream = await response.Content.ReadAsStreamAsync();
            await inputStream.CopyToAsync(outputStream, bufferLength, progress, length, ct);
            return outputStream;
        }
        public async Task<IEnumerable<IPeerShareFolder>> GetShareFolders()
        {
            var api = GetUnderlyingApi();
            return await api.GetShares();
        }
        public async Task<IPeerShareFolder> GetShareFolder(string shareId)
        {
            var api = GetUnderlyingApi();
            return await api.GetShare(shareId);
        }
        public IObservable<float> UploadFileToShare(string shareId, FileInfo file, CancellationToken ct)
        {
            var api = GetUnderlyingApi();
            var fileStream = new FileStream(file.FullName, FileMode.Open);
            var stream = new MemoryStream();
            var streamPart = new StreamPart(stream, file.Name, name: "file");

            var observable = ObservableProgress
                .CreateAsync<float>(reporter => fileStream.CopyToAsync(stream, 1024 * 1024, reporter, file.Length, ct))
                .Do(async x => await api.UploadFileToShare(shareId, streamPart));

            return observable;
        }
    }

}
