using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkInteractableItemAutoAttach : NVRNetworkInteractableItem
    {
        public float PickupAfterSeconds = 0.5f;

        public override void HoveringUpdate(NVRHand hand, float forTime)
        {
            base.HoveringUpdate(hand, forTime);

            if (hand is NVRNetworkHand && ((NVRNetworkHand)hand).IsMine())
            {
                if (IsAttached == false || (IsRemotelyAttached() && NVRNetworkOwnership.Instance.AllowStealingInteraction))
                {
                    if (forTime > PickupAfterSeconds)
                    {
                        hand.BeginInteraction(this);
                    }
                }
            }
        }
    }
}