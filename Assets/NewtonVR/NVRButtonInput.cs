using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRButtonInputs
    {
        /// <summary>Is true ONLY on the frame that the button is first pressed down</summary>
        public bool PressDown;

        /// <summary>Is true ONLY on the frame that the button is released after being pressed down</summary>
        public bool PressUp;

        /// <summary>Is true WHENEVER the button is pressed down</summary>
        public bool IsPressed;

        /// <summary>Is true ONLY on the frame that the button is first touched</summary>
        public bool TouchDown;

        /// <summary>Is true ONLY on the frame that the button is released after being touched</summary>
        public bool TouchUp;

        /// <summary>Is true WHENEVER the button is being touched</summary>
        public bool IsTouched;

        /// <summary>x,y axis generally for the touchpad. trigger uses x</summary>
        public Vector2 Axis;

        /// <summary>x axis from Axis</summary>
        public float SingleAxis;
    }
}
