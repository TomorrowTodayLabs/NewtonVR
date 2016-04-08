using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    [RequireComponent (typeof(NVRTeleporter))]
    public class NVRExampleTeleportGun : NVRInteractableItem
    {
        public Transform FirePoint;
        [HideInInspector]
        public NVRTeleporter Teleporter;

        private NVRPlayer AttachedPlayer;

        private RaycastHit NewLocationInfo;

        protected override void Start()
        {
            base.Start();
            Teleporter = this.GetComponent<NVRTeleporter>();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);
            AttachedPlayer = hand.GetComponentInParent<NVRPlayer>();
        }

        public override void UseButtonDown()
        {
            base.UseButtonDown();

            Ray TeleportRay = new Ray(FirePoint.position, FirePoint.forward);
            Debug.DrawRay(FirePoint.position, FirePoint.forward, Color.red, 4.0f);
            Physics.Raycast(TeleportRay, out NewLocationInfo);
            Debug.Log(NewLocationInfo.point.ToString());
            Teleporter.Teleport(AttachedPlayer, NewLocationInfo.point);
            
        }
    }
}
