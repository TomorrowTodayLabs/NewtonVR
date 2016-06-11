using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleButtonDropSphere : MonoBehaviour
    {
        public void DropSphere()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(0, 20, 0);
            sphere.transform.localScale = Vector3.one * 0.1f;

            sphere.AddComponent<Rigidbody>();
            sphere.AddComponent<NVRInteractableItem>();
        }
    }
}