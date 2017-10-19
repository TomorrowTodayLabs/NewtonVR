using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace NewtonVR
{
    public class NVRVirtualInputDevice : NVRInputDevice
    {
        private GameObject RenderModel;

		public float radius = 0.5f;
		
        public override void Initialize(NVRHand hand)
        {
			base.Initialize(hand);
        }

        public override float GetAxis1D(NVRButtons button)
        {
                // Do nothing
				return 0f;
        }

        public override Vector2 GetAxis2D(NVRButtons button)
        {
                // Do nothing
				return Vector2.zero;
        }

        public override bool GetPressDown(NVRButtons button)
        {
				// Do nothing
                return false;
        }

        public override bool GetPressUp(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override bool GetPress(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override bool GetTouchDown(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override bool GetTouchUp(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override bool GetTouch(NVRButtons button)
        {
				// Do nothing
                return false;
        }

        public override bool GetNearTouchDown(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override bool GetNearTouchUp(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override bool GetNearTouch(NVRButtons button)
        {
                // Do nothing
				return false;
        }

        public override void TriggerHapticPulse(ushort durationMicroSec = 500, NVRButtons button = NVRButtons.Touchpad)
        {
                // Do nothing
        }

        public override bool IsCurrentlyTracked
        {
            get
            {
                return true;
            }
        }


        public override GameObject SetupDefaultRenderModel()
        {
			RenderModel = Hand.gameObject;

            return RenderModel;
        }

        public override bool ReadyToInitialize()
        {
            return true;
        }

        public override string GetDeviceName()
        {
            if (Hand.HasCustomModel == true)
            {
                return "Custom";
            }
            else if (Hand.IsLeft)
            {
                return "VirtualLeft";
            } else
			{
				return "VirtualRight";
			}
        }

        public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
        {
            Collider[] Colliders = null;

			SphereCollider Collider = GetComponent<SphereCollider>();

			Colliders = new Collider[] { Collider };

            return Colliders;
        }

        public override Collider[] SetupDefaultColliders()
        {
            Collider[] Colliders = null;
            
            SphereCollider Collider = RenderModel.AddComponent<SphereCollider>();
            Collider.isTrigger = true;
            Collider.radius = radius;

            Colliders = new Collider[] { Collider };

            return Colliders;
        }
        
    }
}
