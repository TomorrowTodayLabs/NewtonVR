using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public class NVRTeleporter : MonoBehaviour
    {
        private NVRHand Hand;

        private void Start()
        {
            Hand = this.GetComponent<NVRHand>();
        }
    }
}