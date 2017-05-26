using UnityEngine;
using System.Collections;
using System.Linq;

namespace NewtonVR.Network
{
    public interface NVRNetworkInteractable : NVRNetworkObject
    {
        bool IsRemotelyAttached();
    }
}