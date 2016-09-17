﻿using UnityEngine;

namespace NewtonVR
{
    public class NVRAttachJoint : MonoBehaviour
    {
        public NVRInteractableItem AttachedItem;
        public NVRAttachPoint AttachedPoint;

        public bool IsAttached { get { return AttachedItem != null; } }

        public float PullRange = 0.1f;
        public float AttachRange = 0.1f;
        public float DropDistance = 0.1f;

        protected virtual void OnTriggerStay(Collider col)
        {
            if (IsAttached == false)
            {
                NVRAttachPoint point = AttachPointMapper.GetAttachPoint(col);
                if (point != null && point.IsAttached == false)
                {
                    float distance = Vector3.Distance(point.transform.position, this.transform.position);

                    if (distance < AttachRange)
                    {
                        Attach(point);
                    }
                    else
                    {
                        point.PullTowards(this.transform.position);
                    }
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                FixedUpdateAttached();
            }
        }

        protected virtual void FixedUpdateAttached()
        {
            float distance = Vector3.Distance(AttachedPoint.transform.position, this.transform.position);

            if (distance > DropDistance)
            {
                Detach();
            }
            else
            {
                AttachedPoint.PullTowards(this.transform.position);
            }
        }

        protected virtual void Attach(NVRAttachPoint point)
        {
            point.Attached(this);

            AttachedItem = point.Item;
            AttachedPoint = point;
        }

        protected virtual void Detach()
        {
            AttachedPoint.Detached(this);
            AttachedItem = null;
            AttachedPoint = null;
        }
    }
}