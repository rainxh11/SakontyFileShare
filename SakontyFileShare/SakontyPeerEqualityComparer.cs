using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SakontyFileShare
{
    public class SakontyPeerEqualityComparer : IEqualityComparer<ISakontyPeer>
    {
        public bool Equals(ISakontyPeer x, ISakontyPeer y)
        {
            return x.Hwid == y.Hwid;
        }

        public int GetHashCode([DisallowNull] ISakontyPeer obj)
        {
            unchecked
            {
                return Invio.Hashing.HashCode.From(obj.Hwid);
            }
        }
    }

}
