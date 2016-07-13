using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableItem : NVRInteractable
    {
        [Tooltip("If you have a specific point you'd like the object held at, create a transform there and set it to this variable")]
        public Transform InteractionPoint;
        
        protected Transform PickupTransform;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }

        public override void OnNewPosesApplied()
        {
            base.OnNewPosesApplied();

            if (IsAttached == true)
            {
                Quaternion RotationDelta;
                Vector3 PositionDelta;

                float angle;
                Vector3 axis;

                if (InteractionPoint != null)
                {
                    RotationDelta = AttachedHand.transform.rotation * Quaternion.Inverse(InteractionPoint.rotation);
                    PositionDelta = (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    RotationDelta = PickupTransform.rotation * Quaternion.Inverse(this.transform.rotation);
                    PositionDelta = (PickupTransform.position - this.transform.position);
                }

                RotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                if (angle != 0)
                {
                    Vector3 AngularTarget = angle * axis;
                    this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, AngularTarget, 10f * (deltaPoses * 1000));
                }
                
                Vector3 VelocityTarget = PositionDelta / deltaPoses;
                this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, VelocityTarget, 10f);
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            PickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", this.gameObject.name)).transform;
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
    }
}