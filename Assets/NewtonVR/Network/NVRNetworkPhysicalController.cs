using UnityEngine;
using System.Collections;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkPhysicalController : NVRPhysicalController, NVRNetworkObject
    {
        public abstract bool isMine();
    }
}