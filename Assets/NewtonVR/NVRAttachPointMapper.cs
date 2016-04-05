using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace NewtonVR
{
    public class AttachPointMapper
    {
        private static Dictionary<Collider, NVRAttachPoint> Colliders = new Dictionary<Collider, NVRAttachPoint>();

        public static void Register(Collider col, NVRAttachPoint point)
        {
            Colliders.Add(col, point);
        }

        public static void Deregister(Collider col)
        {
            Colliders.Remove(col);
        }

        public static NVRAttachPoint GetAttachPoint(Collider col)
        {
            NVRAttachPoint point;

            Colliders.TryGetValue(col, out point);

            return point;
        }
    }
}