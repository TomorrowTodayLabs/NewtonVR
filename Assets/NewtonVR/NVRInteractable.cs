﻿using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public abstract class NVRInteractable : MonoBehaviour
    {
        public Rigidbody Rigidbody;

        public bool CanAttach = true;
        
        public bool DisableKinematicOnAttach = true;
        public bool EnableKinematicOnDetach = false;
        public float DropDistance = 1;

        public bool EnableGravityOnDetach = true;

        public NVRHand AttachedHand = null;
        public int GraspTargetCount = 0;

        protected Collider[] Colliders;
        protected Vector3 ClosestHeldPoint;

        

        public virtual bool IsAttached
        {
            get
            {
                return AttachedHand != null;
            }
        }
        public virtual bool IsGraspTarget
        {
            get
            {
                return (GraspTargetCount > 0);
            }
        }

        protected virtual void Awake()
        {   
            if (Rigidbody == null)
                Rigidbody = this.GetComponent<Rigidbody>();

            if (Rigidbody == null)
            {
                Debug.LogError("There is no rigidbody attached to this interactable.");
            }

            Colliders = this.GetComponentsInChildren<Collider>();
        }

        protected virtual void Start()
        {
            NVRInteractables.Register(this, Colliders);
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                float shortestDistance = float.MaxValue;

                for (int index = 0; index < Colliders.Length; index++)
                {
                    //todo: this does not do what I think it does.
                    Vector3 closest = Colliders[index].ClosestPointOnBounds(AttachedHand.transform.position);
                    float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        ClosestHeldPoint = closest;
                    }
                }

                if (shortestDistance > DropDistance)
                {
                    DroppedBecauseOfDistance();
                }
            }
        }

        //Remove items that go too high or too low.
        protected virtual void Update()
        {
            if (this.transform.position.y > 10000 || this.transform.position.y < -10000)
            {
                if (AttachedHand != null)
                    AttachedHand.EndInteraction(this);

                Destroy(this.gameObject);
            }
        }

        public virtual void BeginInteraction(NVRHand hand)
        {
            AttachedHand = hand;

            if (DisableKinematicOnAttach == true)
            {
                Rigidbody.isKinematic = false;
            }
        }

        public virtual void InteractingUpdate(NVRHand hand)
        {
            if (hand.UseButtonUp == true)
            {
                UseButtonUp();
            }

            if (hand.UseButtonDown == true)
            {
                UseButtonDown();
            }
        }

        public void ForceDetach()
        {
            if (AttachedHand != null)
                AttachedHand.EndInteraction(this);

            if (AttachedHand != null)
                EndInteraction();
        }

        public virtual void EndInteraction()
        {
            AttachedHand = null;
            ClosestHeldPoint = Vector3.zero;

            if (EnableKinematicOnDetach == true)
            {
                Rigidbody.isKinematic = true;
            }

            if (EnableGravityOnDetach == true)
            {
                Rigidbody.useGravity = true;
            }
        }

        public virtual void GraspTargetBegin(NVRHand hand)
        {
            GraspTargetCount++;
        }

        public virtual void GraspTargetEnd(NVRHand hand)
        {
            GraspTargetCount--;
        }

        protected virtual void DroppedBecauseOfDistance()
        {
            AttachedHand.EndInteraction(this);
        }

        public virtual void UseButtonUp()
        {

        }

        public virtual void UseButtonDown()
        {

        }

        protected virtual void OnDestroy()
        {
            ForceDetach();
            NVRInteractables.Deregister(this);
        }
    }
}
