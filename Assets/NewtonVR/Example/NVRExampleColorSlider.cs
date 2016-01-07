using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleColorSlider : MonoBehaviour
    {
        public Color From;
        public Color To;

        public Renderer Result;

        public NVRSlider Slider;

        private void Update()
        {
            Result.material.color = Color.Lerp(From, To, Slider.CurrentValue);
        }
    }
}