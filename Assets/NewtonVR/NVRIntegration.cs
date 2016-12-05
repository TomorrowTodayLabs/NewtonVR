using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public abstract class NVRIntegration
    {
        protected NVRPlayer Player;

        public abstract void Initialize(NVRPlayer player);

        public abstract Vector3 GetPlayspaceBounds();

        public abstract bool IsHmdPresent();
    }
}