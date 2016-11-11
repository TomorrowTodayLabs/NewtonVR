using System;

namespace NewtonVR
{
    public enum NVRButtons
    {
        System,
        ApplicationMenu,
        Grip,
        DPad_Left,
        DPad_Up,
        DPad_Right,
        DPad_Down,
        A,
        B,
        X,
        Y,
        Axis0,
        Axis1,
        Axis2,
        Axis3,
        Axis4,
        Touchpad,
        Trigger,
        Back,
        Stick
    }

    public class NVRButtonsHelper
    {
        private static NVRButtons[] array = null;
        public static NVRButtons[] Array
        {
            get
            {
                if (array == null)
                {
                    array = (NVRButtons[])System.Enum.GetValues(typeof(NVRButtons));
                }
                return array;
            }
        }
    }
}
