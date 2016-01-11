using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleColorLever
        : MonoBehaviour
    {
        public Color From;
        public Color To;

        public Renderer Result;

        public NVRLever Lever;

        private void Update()
        {
            Result.material.color = Color.Lerp(From, To, Lever.CurrentValue);
        }
    }
}