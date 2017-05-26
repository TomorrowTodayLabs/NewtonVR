using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace NewtonVR
{
    public class NVRProxyInputDevice : NVRInputDevice
    {
        private GameObject RenderModel;

        private Dictionary<NVRButtons, NVRButtonInputs> ButtonMapping = new Dictionary<NVRButtons, NVRButtonInputs>(new NVRButtonsComparer());

        public override void Initialize(NVRHand hand)
        {
            base.Initialize(hand);

            ButtonMapping = new Dictionary<NVRButtons, NVRButtonInputs>();
        }

        public override float GetAxis1D(NVRButtons button)
        {
            return ButtonMapping[button].SingleAxis;
        }

        public override Vector2 GetAxis2D(NVRButtons button)
        {
            return ButtonMapping[button].Axis;
        }

        public override bool GetPressDown(NVRButtons button)
        {
            return ButtonMapping[button].PressDown;
        }

        public override bool GetPressUp(NVRButtons button)
        {
            return ButtonMapping[button].PressUp;
        }

        public override bool GetPress(NVRButtons button)
        {
            return ButtonMapping[button].IsPressed;
        }

        public override bool GetTouchDown(NVRButtons button)
        {
            return ButtonMapping[button].TouchDown;
        }

        public override bool GetTouchUp(NVRButtons button)
        {
            return ButtonMapping[button].TouchUp;
        }

        public override bool GetTouch(NVRButtons button)
        {
            return ButtonMapping[button].IsTouched;
        }

        public override bool GetNearTouchDown(NVRButtons button)
        {
            return ButtonMapping[button].NearTouchDown;
        }

        public override bool GetNearTouchUp(NVRButtons button)
        {
            return ButtonMapping[button].NearTouchUp;
        }

        public override bool GetNearTouch(NVRButtons button)
        {
            return ButtonMapping[button].IsNearTouched;
        }

        public override void TriggerHapticPulse(ushort durationMicroSec = 500, NVRButtons button = NVRButtons.Touchpad)
        {
            //lul
        }

        protected bool isCurrentlyTracked = false;
        public override bool IsCurrentlyTracked
        {
            get
            {
                return isCurrentlyTracked;
            }
        }


        public override GameObject SetupDefaultRenderModel()
        {
            return null;
        }

        public override bool ReadyToInitialize()
        {
            return true;
        }

        protected string DeviceName;
        public override string GetDeviceName()
        {
            if (Hand.HasCustomModel == true)
            {
                return "Custom";
            }
            else
            {
                return DeviceName;
            }
        }

        public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
        {
            return null;
        }

        public override Collider[] SetupDefaultColliders()
        {
            return null;
        }
    }
}