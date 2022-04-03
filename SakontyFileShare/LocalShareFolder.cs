using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Akavache;
using System.Reactive.Threading.Tasks;
using System.Linq;

namespace SakontyFileShare
{
    public class LocalShareFolder : ILocalShareFolder
    {
        public LocalShareFolder(DirectoryInfo dirInfo)
        {
            FolderInfo = dirInfo;
        }
        public async Task Refresh()
        {
            var createdBy = await BlobCache.InMemory.GetObject<IPeerIdentity>("identity").ToTask();

            Files = FolderInfo.EnumerateFiles("*", new EnumerationOptions()
            {
                RecurseSubdirectories = true,
            }).Select(file => new LocalFile(file, createdBy).ToPeerFile());
               
        }
        public DirectoryInfo FolderInfo { get; set; }
        public string Name { get; set; }
        public long FreeSpace { get; set; }
        public IEnumerable<IPeerFile> Files { get; set; }
        public long UsedSpace { get; set; }
    }
}
