using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleColorLever
        : MonoBehaviour
    {
        [SerializeField] Color From, To;
        [SerializeField] Renderer Result;
        [SerializeField] NVRLever Lever;

        private void Update()
        {
            Result.material.color = Color.Lerp(From, To, Lever.CurrentValue);
        }
    }
}