using UnityEngine;
using System.Collections;
using System.Linq;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkInteractableItem : NVRInteractableItem, NVRNetworkInteractable
    {
        public abstract bool IsMine();
        
        public virtual bool IsRemotelyAttached()
        {
            if (AttachedHand != null)
            {
                bool remotelyAttached = false;
                for (int handIndex = 0; handIndex < AttachedHands.Count; handIndex++)
                {
                    NVRHand hand = AttachedHands[handIndex];
                    if (hand is NVRNetworkHand)
                    {
                        remotelyAttached |= ((NVRNetworkHand)hand).IsMine() == false;
                    }
                }
                return remotelyAttached;
            }

            return false;
        }

        protected override void Start()
        {
            StartCoroutine(DelayedStart()); //todo: this is a pretty hacky way to do this. We should really move this stuff out of nvrplayer.
        }

        protected virtual IEnumerator DelayedStart()
        {
            while (NVRPlayer.Instance == null)
            {
                yield return null;
            }

            UpdateColliders();

            if (NVRPlayer.Instance.VelocityHistorySteps > 0)
            {
                VelocityHistory = new Vector3?[NVRPlayer.Instance.VelocityHistorySteps];
                AngularVelocityHistory = new Vector3?[NVRPlayer.Instance.VelocityHistorySteps];
            }
        }

        protected override void FixedUpdate()
        {
            if (IsMine())
            {
                base.FixedUpdate();
            }
        }
    }
}