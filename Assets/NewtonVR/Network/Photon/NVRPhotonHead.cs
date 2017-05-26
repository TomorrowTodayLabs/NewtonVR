using UnityEngine;
using System.Collections;

using NewtonVR.Network;

namespace NewtonVR.NetworkPhoton
{
    public class NVRPhotonHead : NVRNetworkHead
    {
        private PhotonView pvCache = null;
        public PhotonView photonView
        {
            get
            {
                if (pvCache == null)
                {
                    pvCache = PhotonView.Get(this);
                }
                return pvCache;
            }
        }

        public override bool IsMine()
        {
            return this.photonView.isMine;
        }
    }
}