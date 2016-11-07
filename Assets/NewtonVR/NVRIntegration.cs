using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public abstract class NVRIntegration : MonoBehaviour
    {
        protected NVRPlayer Player;

        public abstract void Initialize(NVRPlayer player);
    }
}