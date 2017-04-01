using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewtonVR
{
    // This class is not part of SteamVR InteractionSystem. It can be used to invert the
    // output of a linear mapping.
    public class NVRLinearInverter : MonoBehaviour 
    {
        public NVRLinearMapping source;
        public NVRLinearMapping invertedDestination;

        // Update is called once per frame
        private void Update () 
        {
            invertedDestination.value = Mathf.Clamp01(1f - source.value);
        }
    }
}
