using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRButtonInputs
    {
        /// <summary>Is true ONLY on the frame that the button is first pressed down</summary>
        public bool PressDown
        {
            get
            {
                if (PressDownExpired)
                {
                    PressDownCached = (InputDevice != null) ? InputDevice.GetPressDown(NVRbutton) : false;
                    PressDownExpired = false;
                }
                return PressDownCached;
            }
        }

        private bool PressDownCached;
        private bool PressDownExpired = true;

        /// <summary>Is true ONLY on the frame that the button is released after being pressed down</summary>
        public bool PressUp
        {
            get
            {
                if (PressUpExpired)
                {
                    PressUpCached = (InputDevice != null) ? InputDevice.GetPressUp(NVRbutton) : false;
                    PressUpExpired = false;
                }
                return PressUpCached;
            }
        }

        private bool PressUpCached;
        private bool PressUpExpired = true;

        /// <summary>Is true WHENEVER the button is pressed down</summary>
        public bool IsPressed
        {
            get
            {
                if (IsPressedExpired)
                {
                    IsPressedCached = (InputDevice != null) ? InputDevice.GetPress(NVRbutton) : false;
                    IsPressedExpired = false;
                }
                return IsPressedCached;
            }
        }

        private bool IsPressedCached;
        private bool IsPressedExpired = true;

        /// <summary>Is true ONLY on the frame that the button is first touched</summary>
        public bool TouchDown
        {
            get
            {
                if (TouchDownExpired)
                {
                    TouchDownCached = (InputDevice != null) ? InputDevice.GetTouchDown(NVRbutton) : false;
                    TouchDownExpired = false;
                }
                return TouchDownCached;
            }
        }

        private bool TouchDownCached;
        private bool TouchDownExpired = true;

        /// <summary>Is true ONLY on the frame that the button is released after being touched</summary>
        public bool TouchUp
        {
            get
            {
                if (TouchUpExpired)
                {
                    TouchUpCached = (InputDevice != null) ? InputDevice.GetTouchUp(NVRbutton) : false;
                    TouchUpExpired = false;
                }
                return TouchUpCached;
            }
        }

        private bool TouchUpCached;
        private bool TouchUpExpired = true;

        /// <summary>Is true WHENEVER the button is being touched</summary>
        public bool IsTouched
        {
            get
            {
                if (IsTouchedExpired)
                {
                    IsTouchedCached = (InputDevice != null) ? InputDevice.GetTouch(NVRbutton) : false;
                    IsTouchedExpired = false;
                }
                return IsTouchedCached;
            }
        }

        private bool IsTouchedCached;
        private bool IsTouchedExpired = true;

        /// <summary>Is true ONLY on the frame that the button is first near touched</summary>
        public bool NearTouchDown
        {
            get
            {
                if (NearTouchDownExpired)
                {
                    NearTouchDownCached = (InputDevice != null) ? InputDevice.GetNearTouchDown(NVRbutton) : false;
                    NearTouchDownExpired = false;
                }
                return NearTouchDownCached;
            }
        }

        private bool NearTouchDownCached;
        private bool NearTouchDownExpired = true;

        /// <summary>Is true ONLY on the frame that the button is released after being near touched</summary>
        public bool NearTouchUp
        {
            get
            {
                if (NearTouchUpExpired)
                {
                    NearTouchUpCached = (InputDevice != null) ? InputDevice.GetNearTouchUp(NVRbutton) : false;
                    NearTouchUpExpired = false;
                }
                return NearTouchUpCached;
            }
        }

        private bool NearTouchUpCached;
        private bool NearTouchUpExpired = true;

        /// <summary>Is true WHENEVER the button is near being touched</summary>
        public bool IsNearTouched
        {
            get
            {
                if (IsNearTouchedExpired)
                {
                    IsNearTouchedCached = (InputDevice != null) ? InputDevice.GetNearTouch(NVRbutton) : false;
                    IsNearTouchedExpired = false;
                }
                return IsNearTouchedCached;
            }
        }

        private bool IsNearTouchedCached;
        private bool IsNearTouchedExpired = true;

        /// <summary>x,y axis generally for the touchpad. trigger uses x</summary>
        public Vector2 Axis
        {
            get
            {
                if (AxisExpired)
                {
                    AxisCached = (InputDevice != null) ? InputDevice.GetAxis2D(NVRbutton) : Vector2.zero;
                    AxisExpired = false;
                }
                return AxisCached;
            }
        }

        private Vector2 AxisCached;
        private bool AxisExpired = true;

        /// <summary>x axis from Axis</summary>
        public float SingleAxis
        {
            get
            {
                if (SingleAxisExpired)
                {
                    SingleAxisCached = (InputDevice != null) ? InputDevice.GetAxis1D(NVRbutton) : 0f;
                    SingleAxisExpired = false;
                }
                return SingleAxisCached;
            }
        }

        private float SingleAxisCached;
        private bool SingleAxisExpired = true;

        private NVRInputDevice InputDevice;
        private NVRButtons NVRbutton;

        /// <summary>
        /// Reset the cached values for a new frame.
        /// </summary>
        /// <param name="inputDevice">NVRInputDevice</param>
        /// <param name="button">NVRButtons</param>
        public void FrameReset(NVRInputDevice inputDevice, NVRButtons button)
        {
            InputDevice = inputDevice;
            NVRbutton = button;

            PressDownExpired = true;
            PressUpExpired = true;
            IsPressedExpired = true;
            TouchDownExpired = true;
            TouchUpExpired = true;
            IsTouchedExpired = true;
            NearTouchDownExpired = true;
            NearTouchUpExpired = true;
            IsNearTouchedExpired = true;
            AxisExpired = true;
            SingleAxisExpired = true;
        }
    }
}
