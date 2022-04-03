using System;
using System.IO;
using System.Threading.Tasks;

namespace SakontyFileShare
{
    public class LocalFile : ILocalFile
    {
        public LocalFile()
        {

        }
        public static IPeerFile CreatePeerFile(FileInfo file, IPeerIdentity createdBy, string checkSum = null)
        {
            return new LocalFile(file, createdBy, checkSum);
        }
        public IPeerFile ToPeerFile()
        {
            return this;
        }
        public LocalFile(FileInfo file, IPeerIdentity createdBy, string checkSum = null)
        {
            FileInfo = file;
            Name = file.Name;
            Size = file.Length;
            Checksum = checkSum;
            CreatedBy = createdBy;
        }
        private ChecksumStatus _status = ChecksumStatus.ChecksumAbsent;

        public FileInfo FileInfo { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public IPeerIdentity CreatedBy { get; set; }
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

        public ChecksumStatus Status { get => _status; }
        public async Task<ChecksumStatus> VerifyChecksum()
        {
            var result = await ChecksumHelper.CheckFile(this, FileInfo);
            if (result)
            {
                _status = Checksum == null ? ChecksumStatus.ChecksumAbsent : ChecksumStatus.VerifiedValid;
            }
            else
            {
                _status = ChecksumStatus.VerifiedInvalid;
            }
            return _status;
        }

        public async Task<ChecksumStatus> VerifyChecksum(FileInfo fileInfo)
        {
            var result = await ChecksumHelper.CheckFile(this, fileInfo);
            if (result)
            {
                return Checksum == null ? ChecksumStatus.ChecksumAbsent : ChecksumStatus.VerifiedValid;
            }
            else
            {
                return ChecksumStatus.VerifiedInvalid;
            }
        }

#nullable enable
        private string? _checkSum;
        public string? Checksum
        {
            get => _checkSum;
            set
            {
                _checkSum = value;
                if (value == null)
                    _status = ChecksumStatus.ChecksumAbsent;
                else
                    _status = ChecksumStatus.Unverified;
            }
        }
#nullable disable
    }
}
