using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleRGBResult : MonoBehaviour
    {
        public NVRSlider SliderRed;
        public NVRSlider SliderGreen;
        public NVRSlider SliderBlue;

        public Renderer Result;

        public Color ResultColor = Color.black;

        private void Update()
        {
            if (SliderRed != null && SliderGreen != null && SliderBlue != null)
            {
                ResultColor.r = SliderRed.CurrentValue;
                ResultColor.g = SliderGreen.CurrentValue;
                ResultColor.b = SliderBlue.CurrentValue;

                Result.material.color = ResultColor;
            }
        }
    }
}