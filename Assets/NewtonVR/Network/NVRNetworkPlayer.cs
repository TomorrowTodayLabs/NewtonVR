using UnityEngine;
using System.Collections;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkPlayer : NVRPlayer, NVRNetworkObject
    {
        public abstract bool IsMine();

        protected override void Awake()
        {
            if (IsMine())
            {
                base.Awake();
            }
        }

        protected override void Update()
        {
            if (IsMine())
            {
                base.Update();
            }
        }
    }
}
