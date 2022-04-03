using SakontyFileShare;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var server = await SakontyNode
                .CreateNode()
                .StartSeeding(cancellationTokenSource.Token);

            var client = server.GetUnderlyingPeer();

            Console.ReadKey();

        }
    }
}
