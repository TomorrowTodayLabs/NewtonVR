using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace NewtonVR
{
    public abstract class NVRInputDevice : MonoBehaviour
    {
        private NVRHand _hand;
        protected NVRHand Hand
        {
            get
            {
                return _hand;
            }
            set
            {
                _hand = value;
            }
        }

        public virtual void Initialize(NVRHand hand)
        {
            Hand = hand;
        }

        public abstract bool IsCurrentlyTracked { get; }

        public abstract Collider[] SetupDefaultPhysicalColliders(Transform ModelParent);

        public abstract GameObject SetupDefaultRenderModel();

        public abstract bool ReadyToInitialize();

        public abstract Collider[] SetupDefaultColliders();

        public abstract string GetDeviceName();

        public abstract void TriggerHapticPulse(ushort durationMicroSec = 500, NVRButtons button = NVRButtons.Touchpad);

        public abstract float GetAxis1D(NVRButtons button);
        public abstract Vector2 GetAxis2D(NVRButtons button);
        public abstract bool GetPressDown(NVRButtons button);
        public abstract bool GetPressUp(NVRButtons button);
        public abstract bool GetPress(NVRButtons button);
        public abstract bool GetTouchDown(NVRButtons button);
        public abstract bool GetTouchUp(NVRButtons button);
        public abstract bool GetTouch(NVRButtons button);
        public abstract bool GetNearTouchDown(NVRButtons button);
        public abstract bool GetNearTouchUp(NVRButtons button);
        public abstract bool GetNearTouch(NVRButtons button);
    }
}
