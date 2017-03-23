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
                    PressDownCached = InputDevice.GetPressDown(nvrbutton);
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
                    PressUpCached = InputDevice.GetPressUp(nvrbutton);
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
                    IsPressedCached = InputDevice.GetPress(nvrbutton);
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
                    TouchDownCached = InputDevice.GetTouchDown(nvrbutton);
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
                    TouchUpCached = InputDevice.GetTouchUp(nvrbutton);
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
                    IsTouchedCached = InputDevice.GetTouch(nvrbutton);
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
                    NearTouchDownCached = InputDevice.GetNearTouchDown(nvrbutton);
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
                    NearTouchUpCached = InputDevice.GetNearTouchUp(nvrbutton);
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
                    IsNearTouchedCached = InputDevice.GetNearTouch(nvrbutton);
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
                    AxisCached = InputDevice.GetAxis2D(nvrbutton);
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
                    SingleAxisCached = InputDevice.GetAxis1D(nvrbutton);
                    SingleAxisExpired = false;
                }
                return SingleAxisCached;
            }
        }

        private float SingleAxisCached;
        private bool SingleAxisExpired = true;

        private NVRInputDevice InputDevice;
        private NVRButtons nvrbutton;

        /// <summary>
        /// Reset the cached values for a new frame.
        /// </summary>
        /// <param name="inputDevice">NVRInputDevice</param>
        /// <param name="button">NVRButtons</param>
        public void FrameReset(NVRInputDevice inputDevice, NVRButtons button)
        {
            InputDevice = inputDevice;
            nvrbutton = button;

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
