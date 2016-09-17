using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleColorSlider : MonoBehaviour
    {
        [SerializeField] Color From, To;
        [SerializeField] Renderer Result;
        [SerializeField] NVRSlider Slider;

        private void Update()
        {
            Result.material.color = Color.Lerp(From, To, Slider.CurrentValue);
        }
    }
}