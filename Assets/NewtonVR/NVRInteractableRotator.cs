using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableRotator : NVRInteractable
    {
        public float CurrentAngle;

        protected virtual float DeltaMagic { get { return 1f; } }
        protected Transform InitialAttachPoint;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAttached == true)
            {
                Vector3 PositionDelta = (AttachedHand.transform.position - InitialAttachPoint.position) * DeltaMagic;

                this.Rigidbody.AddForceAtPosition(PositionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);
            }

            CurrentAngle = this.transform.localEulerAngles.z;
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            Vector3 closestPoint = Vector3.zero;
            float shortestDistance = float.MaxValue;
            for (int index = 0; index < Colliders.Length; index++)
            {
                Vector3 closest = Colliders[index].bounds.ClosestPoint(AttachedHand.transform.position);
                float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPoint = closest;
                }
            }

            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", this.gameObject.name)).transform;
            //InitialAttachPoint = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            InitialAttachPoint.position = hand.transform.position;
            InitialAttachPoint.rotation = hand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.parent = this.transform;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (InitialAttachPoint != null)
                Destroy(InitialAttachPoint.gameObject);
        }

    }
}