using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream, using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream
        /// will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be
        /// greater than zero. The default size is 81920.</param>
        /// <param name="progress">The progress reporter. Reports the current total amount if
        /// copied bytes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.
        /// The default value is System.Threading.CancellationToken.None.</param>
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, $"{nameof(bufferSize)} has to be greater than zero");
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            if (!source.CanRead)
            {
                throw new ArgumentException($"{nameof(source)} is not readable.", nameof(source));
            }

            if (!destination.CanWrite)
            {
                throw new ArgumentException($"{nameof(destination)} is not writeable.", nameof(source));
            }

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress.Report(totalBytesRead);
            }
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream, using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream
        /// will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater
        /// than zero. The default size is 81920.</param>
        /// <param name="progress">The progress reporter. Reports the current relative progress
        /// in percent. For that the source stream has to support the <see cref="Stream.Length"/> property.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The
        /// default value is System.Threading.CancellationToken.None.</param>
        public static Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<float> progress, long length = 0, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            long totalLength;
            try
            {
                totalLength = length == 0 ? source.Length : length;
            }
            catch (NotSupportedException ex)
            {
                throw new ArgumentException($"Length of {nameof(source)}is needed for relative progress.", nameof(source), ex);
            }

            var absoluteProgress = new Progress<long>(totalBytesCopied => {
                progress.Report((float)totalBytesCopied / totalLength);
            });

            return source.CopyToAsync(destination, bufferSize, absoluteProgress, cancellationToken);
        }
    }
}