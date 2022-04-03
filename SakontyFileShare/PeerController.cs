using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using EmbedIO;
using EmbedIO.Routing;
using Akavache;
using System.Reactive.Threading.Tasks;
using HttpMultipartParser;

namespace SakontyFileShare
{
    public class PeerController : WebApiController
    {

        [Route(HttpVerbs.Get, "/files/{id}")]
        public async Task DownloadFile(string id)
        {
            try
            {
                var file = await BlobCache.LocalMachine.GetObject<ILocalFile>(id).ToTask();

                using(var fs = new FileStream(file.FileInfo.FullName, FileMode.Open))
                using (var stream = HttpContext.OpenResponseStream(buffered: true))
                {
                    await stream.WriteAsync(new byte[fs.Length]);
                }
            }
            catch(Exception ex)
            {
                throw HttpException.NotFound(ex.Message);
            }
        }

        [Route(HttpVerbs.Get, "/files")]
        public async Task<IEnumerable<IPeerFile>> GetFiles()
        {
            var files = await BlobCache.LocalMachine.GetAllObjects<ILocalFile>().ToTask();
            return files;
        }

        [Route(HttpVerbs.Get, "/shares/")]
        public async Task<IEnumerable<IPeerShareFolder>> GetShares()
        {
            var shares = await BlobCache.LocalMachine.GetAllObjects<IPeerShareFolder>().ToTask();
            return shares;
        }

        [Route(HttpVerbs.Get, "/shares/{id}")]
        public async Task<IPeerShareFolder> GetShare(string id)
        {
            try
            {
                var share = await BlobCache.LocalMachine.GetObject<IPeerShareFolder>(id).ToTask();
                return share;
            }
            catch(Exception ex)
            {
                throw HttpException.NotFound(ex.Message);
            }
        }

        [Route(HttpVerbs.Post, "/shares/{id}")]
        public async Task UploadFile(string id)
        {
            try
            {
                var identity = await BlobCache.InMemory.GetObject<IPeerIdentity>("identity").ToTask();

                var share = await BlobCache.LocalMachine.GetObject<ILocalShareFolder>(id).ToTask();

                var parser = await MultipartFormDataParser.ParseAsync(Request.InputStream);
                var file = parser.Files[0];
                var contentLength = Request.ContentLength64;

                var destinationFile = new FileInfo(@$"{share.FolderInfo.FullName}\{file.FileName}");
                if (destinationFile.Exists)
                {
                    throw HttpException.NotAcceptable($"File: {file.FileName} already exists in share '{identity.MachineName}{share.Name}'");
                }
                using (var fileStream = new FileStream(destinationFile.FullName, FileMode.CreateNew))
                {
                    await file.Data.CopyToAsync(fileStream, 1024 * 1024);
                }

            }
            catch(Exception ex)
            {
                throw HttpException.NotFound(ex.Message);
            }
        }

        [Route(HttpVerbs.Get, "/echo")]
        public async Task<IPeerIdentity> Echo()
        {
            try
            {
                var identity = await BlobCache.InMemory.GetObject<IPeerIdentity>("identity").ToTask();
                return identity;
            }
            catch
            {
                throw HttpException.InternalServerError();
            }
        }
    }
}
