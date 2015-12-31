using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class ColorSlider : MonoBehaviour
    {
        public Renderer From;
        public Renderer To;

        public Renderer Result;

        public NVRInteractableSlider Slider;

        private void Update()
        {
            Result.material.color = Color.Lerp(From.material.color, To.material.color, Slider.CurrentValue);
        }
    }
}