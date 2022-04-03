using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SakontyFileShare
{
    public class PeerFileEqualityComparer : IEqualityComparer<IPeerFile>
    {
        public bool Equals(IPeerFile x, IPeerFile y)
        {
            var left = string.IsNullOrEmpty(x.Checksum) || string.IsNullOrEmpty(y.Checksum) ? x.Id : x.Checksum;
            var right = string.IsNullOrEmpty(x.Checksum) || string.IsNullOrEmpty(y.Checksum) ? y.Id : y.Checksum;

            return left == right;
        }

        public int GetHashCode([DisallowNull] IPeerFile obj)
        {
            unchecked
            {

                return string.IsNullOrEmpty(obj.Checksum) ? Invio.Hashing.HashCode.From(obj.Id) : Invio.Hashing.HashCode.From(obj.Checksum);
            }
        }
    }
}
