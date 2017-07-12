﻿using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRSlider : NVRInteractable
    {
        [Tooltip("Set to zero when the slider is at StartPoint. Set to one when the slider is at EndPoint.")]
        public float CurrentValue = 0f;

        [Tooltip("A transform at the position of the zero point of the slider")]
        public Transform StartPoint;

        [Tooltip("A transform at the position of the one point of the slider")]
        public Transform EndPoint;
        
        protected float AttachedPositionMagic = 3000f;

        protected Transform PickupTransform;
        protected Vector3 SliderPath;

        protected override void Awake()
        {
            base.Awake();

            if (StartPoint == null)
            {
                Debug.LogError("This slider has no StartPoint.");
            }
            if (EndPoint == null)
            {
                Debug.LogError("This slider has no EndPoint.");
            }

            this.transform.position = Vector3.Lerp(StartPoint.position, EndPoint.position, CurrentValue);
            SliderPath = EndPoint.position - StartPoint.position;
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                bool dropped = CheckForDrop();

                if (dropped == false)
                {
                    Vector3 PositionDelta = (PickupTransform.position - this.transform.position);

                    Vector3 velocity = PositionDelta * AttachedPositionMagic * Time.deltaTime;
                    this.Rigidbody.velocity = ProjectVelocityOnPath(velocity, SliderPath);
                }
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

            PickupTransform = new GameObject(string.Format("[{0}] PickupTransform", this.gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;
        }

        public override void EndInteraction(NVRHand hand)
        {
            base.EndInteraction(hand);

            if (PickupTransform != null)
                Destroy(PickupTransform.gameObject);
        }

        protected Vector3 ProjectVelocityOnPath(Vector3 velocity, Vector3 path)
        {
            return Vector3.Project(velocity, path);
        }
    }
}

