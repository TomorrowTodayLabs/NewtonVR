using UnityEngine;
using System.Collections;
using NewtonVR.Network;
using System;
using System.Linq;

namespace NewtonVR.NetworkPhoton
{
    public class NVRPhotonHand : NVRNetworkHand
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

        public override void PreInitialize(NVRPlayer player)
        {
            base.PreInitialize(player);

            if (isMine() == false)
            {
                int handID = photonView.viewID;
                if (photonView.owner.CustomProperties.ContainsKey(handID))
                {
                    byte[] handData = (byte[])photonView.owner.CustomProperties[handID];
                    ParseRenderModelData(handData);

                    Initialize();
                }
            }
        }

        protected override void ForceRemoteDrop(NVRInteractable interactable)
        {
            PhotonView interactablePhotonView = NVRPhotonOwnership.PInstance.GetPhotonView(interactable);
            for (int handIndex = 0; handIndex < interactable.AttachedHands.Count; handIndex++)
            {
                NVRPhotonHand attachedHand = (NVRPhotonHand)interactable.AttachedHands[handIndex];
                attachedHand.photonView.RPC("PhotonForceDrop", PhotonTargets.All, interactablePhotonView.viewID);
            }
        }

        protected override void SelfOnBeginInteraction(NVRInteractable interactable)
        {
            PhotonView interactablePhotonView = NVRPhotonOwnership.PInstance.GetPhotonView(interactable);
            photonView.RPC("PhotonBeginInteraction", PhotonTargets.Others, interactablePhotonView.viewID);
        }

        protected override void SelfOnEndInteraction(NVRInteractable interactable)
        {
            PhotonView interactablePhotonView = NVRPhotonOwnership.PInstance.GetPhotonView(interactable);
            photonView.RPC("PhotonEndInteraction", PhotonTargets.Others, interactablePhotonView.viewID);
        }

        protected override void SendRenderModelData(byte[] data)
        {
            ExitGames.Client.Photon.Hashtable handPropertyHash = new ExitGames.Client.Photon.Hashtable();
            handPropertyHash.Add(photonView.viewID, data);
            PhotonNetwork.player.SetCustomProperties(handPropertyHash);
            photonView.RPC("PhotonLoadRemoteRenderModel", PhotonTargets.Others, PhotonNetwork.player.ID, photonView.viewID);
        }

        [PunRPC]
        private void PhotonLoadRemoteRenderModel(int playerID, int handID)
        {
            PhotonPlayer remotePlayer = PhotonNetwork.otherPlayers.First(player => player.ID == playerID);
            byte[] handData = (byte[])remotePlayer.CustomProperties[handID];
            ParseRenderModelData(handData);

            Initialize();
        }


        [PunRPC]
        private void PhotonBeginInteraction(int interactableViewId)
        {
            PhotonView interactableView = PhotonView.Find(interactableViewId);
            NVRInteractable interactable = NVRPhotonOwnership.PInstance.GetInteractable(interactableView);
            this.BeginInteraction(interactable);
        }

        [PunRPC]
        private void PhotonEndInteraction(int interactableViewId)
        {
            PhotonView interactableView = PhotonView.Find(interactableViewId);
            NVRInteractable interactable = NVRPhotonOwnership.PInstance.GetInteractable(interactableView);
            this.EndInteraction(interactable);
        }

        [PunRPC]
        private void PhotonForceDrop(int interactableViewId)
        {
            if (isMine())
            {
                PhotonView interactableView = PhotonView.Find(interactableViewId);
                EndInteraction(interactableView.GetComponent<NVRInteractable>());
            }
        }
    }
}