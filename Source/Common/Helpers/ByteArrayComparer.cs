using System.Collections;
using System.Collections.Generic;

namespace Common.Helpers
{
    /// <summary>
    ///    Tests for equality between two byte arrays based on their value
    ///    sequences.
    ///	<param name = "obj1">A byte array to test for equality against obj2.</param>
    /// <param name = "obj2">A byte array to test for equality againts obj1.</param>
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        private ByteArrayComparer _default;

        public ByteArrayComparer Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new ByteArrayComparer();
                }

                return _default;
            }
        }

        public bool Equals(byte[] obj1, byte[] obj2)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(obj1, obj2);
        }

        public int GetHashCode(byte[] obj)
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }
}
