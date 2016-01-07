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


        private void Update()
        {
            Result.material.color = new Color(SliderRed.CurrentValue, SliderGreen.CurrentValue, SliderBlue.CurrentValue);
        }
    }
}