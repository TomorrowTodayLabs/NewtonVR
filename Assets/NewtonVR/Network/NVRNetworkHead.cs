using UnityEngine;
using System.Collections;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkHead : NVRHead, NVRNetworkObject
    {
        public abstract bool isMine();
    }
}
