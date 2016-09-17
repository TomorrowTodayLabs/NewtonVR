using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleRGBResult : MonoBehaviour
    {
        [SerializeField] NVRSlider SliderRed, SliderGreen, SliderBlue;
        [SerializeField] Renderer Result;

        private void Update()
        {
            Result.material.color = new Color(SliderRed.CurrentValue, SliderGreen.CurrentValue, SliderBlue.CurrentValue);
        }
    }
}