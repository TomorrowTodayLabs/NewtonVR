using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    /// <summary>
    /// This interactable item script clips through other colliders. If you don't want your item to respect other object's positions 
    /// and have it go through walls/floors/etc then you can use this.
    /// </summary>
    public class NVRInteractableItemClippable : NVRInteractable
    {
        [Tooltip("If you have a specific point you'd like the object held at, create a transform there and set it to this variable")]
        public Transform InteractionPoint;
        
        protected Transform PickupTransform;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }

        protected void FixedUpdate()
        {
            if (IsAttached == true)
            {
                Vector3 TargetPosition;
                Quaternion TargetRotation;

                if (InteractionPoint != null)
                {
                    TargetRotation = AttachedHand.transform.rotation * Quaternion.Inverse(InteractionPoint.localRotation);
                    TargetPosition = this.transform.position + (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    TargetRotation = PickupTransform.rotation;
                    TargetPosition = PickupTransform.position;
                }

                this.Rigidbody.MovePosition(TargetPosition);
                this.Rigidbody.MoveRotation(TargetRotation);
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            this.Rigidbody.isKinematic = true;

            PickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", this.gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            this.Rigidbody.isKinematic = false;

            if (PickupTransform != null)
                Destroy(PickupTransform.gameObject);
        }

    }
}