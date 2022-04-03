using System.Net;

namespace SakontyFileShare
{
    public class PeerIdentity : IPeerIdentity
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

    }
}
