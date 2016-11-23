using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

#if NVR_Oculus
namespace NewtonVR
{
    public class NVROculusIntegration : NVRIntegration
    {
        private OVRBoundary boundary;
        private OVRBoundary Boundary
        {
            get
            {
                if (boundary == null)
                {
                    boundary = new OVRBoundary();
                }
                return boundary;
            }
        }

        public override void Initialize(NVRPlayer player)
        {
            Player = player;
            Player.gameObject.SetActive(false);

            OVRManager manager = Player.gameObject.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            OVRCameraRig rig = Player.gameObject.AddComponent<OVRCameraRig>();

            NVRHelpers.SetProperty(rig, "trackingSpace", Player.transform, true);
            NVRHelpers.SetProperty(rig, "leftHandAnchor", Player.LeftHand.transform, true);
            NVRHelpers.SetProperty(rig, "rightHandAnchor", Player.RightHand.transform, true);
            NVRHelpers.SetProperty(rig, "centerEyeAnchor", Player.Head.transform, true);

            Player.gameObject.SetActive(true);
        }

        private Vector3 PlayspaceBounds = Vector3.zero;
        public override Vector3 GetPlayspaceBounds()
        {
            bool configured = Boundary.GetConfigured();
            if (configured == true)
            {
                PlayspaceBounds = Boundary.GetDimensions(OVRBoundary.BoundaryType.OuterBoundary);
            }

            return PlayspaceBounds;
        }
    }
}
#else
namespace NewtonVR
{
    public class NVROculusIntegration : NVRIntegration
    {
        public override void Initialize(NVRPlayer player)
        {
        }

        public override Vector3 GetPlayspaceBounds()
        {
            return Vector3.zero;
        }
    }
}
#endif