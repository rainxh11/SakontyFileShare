using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Butterfly.Web.EmbedIO;
using Butterfly.Web.Channel;
using Butterfly.Web.WebApi;
using Butterfly.Util;
using Refit;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Threading;

namespace SakontyFileShare
{

    public interface IPeerApi
    {
        [Post("/peer/shares/{id}")]
        Task UploadFileToShare(string id, [AliasAs("file")] StreamPart stream);

        [Get("/peer/shares/")]
        Task<IEnumerable<IPeerShareFolder>> GetShares();

        [Get("/peer/shares/{id}")]
        Task<IPeerShareFolder> GetShare(string id);

        [Get("/peer/files/{id}")]
        Task<HttpResponseMessage> DownloadFile(string id);

        [Get("/peer/files")]
        Task<IEnumerable<IPeerFile>> GetFiles();

        [Get("/peer/echo")]
        Task<IPeerIdentity> Echo();
    }
    public interface ILocalShareFolder : IPeerShareFolder
    {
        DirectoryInfo FolderInfo { get; set; }
        Task Refresh();
    }
    public interface IPeerShareFolder
    {
        string Name { get; set; }
        long FreeSpace { get; set; }
        IEnumerable<IPeerFile> Files { get; set; }
        long UsedSpace { get; set; }
    }
    public interface ILocalFile : IPeerFile
    {
        FileInfo FileInfo { get; set; }
        Task<ChecksumStatus> VerifyChecksum();
    }
    public enum ChecksumStatus
    {
        ChecksumAbsent,
        Unverified,
        VerifiedValid,
        VerifiedInvalid,
    }
    public interface IPeerFile
    {
        string Name { get; set; }
        long Size { get; set; }
        string Id { get; set; }
        IPeerIdentity CreatedBy { get; set; }
        DateTimeOffset Date { get; set; }
        ChecksumStatus Status { get; }
        Task<ChecksumStatus> VerifyChecksum(FileInfo fileInfo);
#nullable enable
        string? Checksum { get; set; }
#nullable disable

    }
    public interface IPeerIdentity
    {
        string Name { get; set; }
        string MachineName { get; set; }
        string Hwid { get; set; }
        string Address { get; set; }
        int Port { get; set; }
        bool Blocked { get; set; }
        IPeerIdentity SetBlockFlag(bool flag);
    }
    public interface ISakontyPeer : IPeerIdentity
    {
        bool IsSeed { get; }
        bool IsConnected { get; }
        Task Connect();
        IPeerApi GetUnderlyingApi();
        IPeerIdentity GetUnderlyingIdentity();
        Task<IEnumerable<IPeerFile>> GetFiles();
        Task<Stream> StreamFile(IPeerFile file, long startPosition, int bufferLength, IProgress<float> progress, CancellationToken ct);
        Task<Stream> StreamFile(IPeerFile file, IProgress<float> progress, CancellationToken ct);
        Task<IObservable<float>> DownloadFile(IPeerFile file, string destination, CancellationToken ct);
        ISakontyPeer CreatePeer(IPeerIdentity peerIdentity);
        Task<IEnumerable<IPeerShareFolder>> GetShareFolders();
        Task<IPeerShareFolder> GetShareFolder(string shareId);
        IObservable<float> UploadFileToShare(string shareId, FileInfo file, CancellationToken ct);

    }
    public interface INodeCreator
    {
        Task<ISakontyNode> StartSeeding(CancellationToken cancellationToken, IPAddress address, int port = 5050);
        Task<ISakontyNode> StartSeeding(CancellationToken cancellationToken, string bindIp = "0.0.0.0.", int port = 5050);
    }
    public interface ISakontySeed
    {
        bool IsListening { get; }
        ISakontyPeer GetUnderlyingPeer();
        Task<IPeerFile> AddFile(FileInfo file, bool calculateCheckSum = true);
        Task<IPeerFile> AddFile(FileInfo file, TimeSpan expiration, bool calculateCheckSum = true);
        Task<IPeerFile> AddFile(FileInfo file, DateTime expirationDate, bool calculateCheckSum = true);
        Task<IPeerShareFolder> AddShareFolder(DirectoryInfo folder);
        Task<IPeerShareFolder> AddShareFolder(DirectoryInfo folder, TimeSpan expiration);
        Task<IPeerShareFolder> AddShareFolder(DirectoryInfo folder, DateTime expiration);
        Task<IPeerFile> UpdateFileExpiration(string fileId, DateTime expirationDate);
        Task<bool> RemoveFile(string fileId);
        void StopSeeding();
        IEnumerable<ISakontyPeer> GetPeers();
        IAsyncEnumerable<ISakontyPeer> ConnectPeers(params IPeerIdentity[] peers);
        IAsyncEnumerable<ISakontyPeer> ConnectPeers(params KeyValuePair<string, int>[] addresses);
        IAsyncEnumerable<ISakontyPeer> ConnectPeers(params IPEndPoint[] endpoints);
        Task<bool> BlockPeer(params IPeerIdentity[] peers);
        Task<bool> BlockPeer(TimeSpan duration, params IPeerIdentity[] peers);
        Task<bool> BlockPeer(DateTime unblockDate, params IPeerIdentity[] peers);
        Task<bool> UnblockPeer(params IPeerIdentity[] peers);
        Task ClearCache();
        Task<IPeerIdentity> SetIdentityName(string name);
    }
    public interface ISakontyUtil
    {
        IObservable<IPeerIdentity> ScanForPeers(string ip, int port = 5050);
        Task<bool> CheckFile(IPeerFile peerFile, FileInfo file);
        Task<string> CreateChecksum(FileInfo file);
        string GetHardwardID();
        string GetMachineName();
    }
    public interface ISakontyNode : INodeCreator, ISakontyPeer, ISakontySeed, ISakontyUtil
    {

    }
}
