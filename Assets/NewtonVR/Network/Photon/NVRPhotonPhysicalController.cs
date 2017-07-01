using UnityEngine;
using System.Collections;
using NewtonVR.Network;
using System;

namespace NewtonVR.NetworkPhoton
{
    public class NVRPhotonPhysicalController : NVRNetworkPhysicalController
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

        private bool cachedInitialState;
        public override void Initialize(NVRHand trackingHand, bool initialState)
        {
            if (isMine())
            {
                base.Initialize(trackingHand, initialState);
            }
            else
            {
                Hand = trackingHand;
                cachedInitialState = initialState;
                photonView.RPC("PhotonInitializePhysicalHand", PhotonTargets.Others, PhotonNetwork.player.ID, photonView.viewID);
            }
        }

        [PunRPC]
        private void PhotonInitializePhysicalHand(int playerID, int handID)
        {
            PhotonView physicalHandView = PhotonView.Find(handID);
            PhysicalController = physicalHandView.gameObject;

            base.Initialize(Hand, cachedInitialState);
        }

        protected override GameObject InstantiatePhysicalControllerGameobject()
        {
            if (isMine())
            {
                return PhotonNetwork.Instantiate("NVRPhysicalHand", Hand.transform.position, Hand.transform.rotation, 0);
            }
            else
            {
                return PhysicalController;
            }
        }
    }
}