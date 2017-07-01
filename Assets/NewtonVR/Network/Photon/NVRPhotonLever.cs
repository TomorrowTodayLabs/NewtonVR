using UnityEngine;
using System.Collections;
using System;
using NewtonVR.Network;

namespace NewtonVR.NetworkPhoton
{
    public class NVRPhotonLever : NVRNetworkLever
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

        public override bool isMine()
        {
            return this.photonView.isMine;
        }
    }
}
