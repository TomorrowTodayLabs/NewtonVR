using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableSlider : NVRInteractable
    {
        public float CurrentValue = 0f;

        public Transform StartPoint;
        public Transform EndPoint;
        
        protected float AttachedPositionMagic = 3000f;

        protected Transform PickupTransform;
        protected Vector3 SliderPath;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.freezeRotation = true;

            if (StartPoint == null)
            {
                StartPoint = this.transform;
            }
            if (EndPoint == null)
            {
                EndPoint = this.transform;
            }

            this.transform.position = StartPoint.position;
            SliderPath = EndPoint.position - StartPoint.position;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAttached == true)
            {
                Vector3 PositionDelta = (PickupTransform.position - this.transform.position);

                Vector3 velocity = PositionDelta * AttachedPositionMagic * Time.fixedDeltaTime;
                this.Rigidbody.velocity = ProjectVelocityOnPath(velocity, SliderPath);
            }

            if (this.transform.hasChanged == true)
            {
                float totalDistance = Vector3.Distance(StartPoint.position, EndPoint.position);
                float distance = Vector3.Distance(StartPoint.position, this.transform.position);
                CurrentValue = distance / totalDistance;

                this.transform.hasChanged = false;
            }
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

            PickupTransform = new GameObject("PickupTransform: " + this.gameObject.name).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (PickupTransform != null)
                Destroy(PickupTransform.gameObject);
        }

        protected Vector3 ProjectVelocityOnPath(Vector3 velocity, Vector3 path)
        {
            return Vector3.Project(velocity, path);
        }
    }
}

