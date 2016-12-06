using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewtonVR
{
    public struct NVRButtonsComparer : IEqualityComparer<NVRButtons>
    {
        public bool Equals(NVRButtons x, NVRButtons y)
        {
            return x == y;
        }

        public int GetHashCode(NVRButtons obj)
        {
            return (int)obj;
        }
    }
}
