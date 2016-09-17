using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleRGBResult : MonoBehaviour
    {
        public NVRSlider SliderRed, SliderGreen, SliderBlue;

        public Renderer Result;


        private void Update()
        {
            Result.material.color = new Color(SliderRed.CurrentValue, SliderGreen.CurrentValue, SliderBlue.CurrentValue);
        }
    }
}