using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace NewtonVR
{
    public class NVRInteractableItemAutoAttach : NVRInteractableItem
    {
        public float PickupAfterSeconds = 0.5f;

        public override void HoveringUpdate(NVRHand hand, float forTime)
        {
            base.HoveringUpdate(hand, forTime);

            if (IsAttached == false)
            {
                if (forTime > PickupAfterSeconds)
                {
                    hand.BeginInteraction(this);
                }
            }
        }
    }
}