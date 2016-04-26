using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableItemAdjustableMagic : NVRInteractableItem {

        public float AxisX;
        public float AxisY;
	
	    // Update is called once per frame
	    protected override void Update () {
            base.Update();
            if (IsAttached)
            {
                if (AttachedHand.TouchpadButtonPressed)
                {
                    AxisX = AttachedHand.TouchpadButtonAxis.x;
                    AxisY = AttachedHand.TouchpadButtonAxis.y;
                }
            }
	    }
    }
}


