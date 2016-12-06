using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;

namespace NewtonVR
{
    public class NVRAttachPoint : MonoBehaviour
    {
        private const float MaxVelocityChange = 5f;
        private const float MaxAngularVelocityChange = 10f;
        private const float VelocityMagic = 3000f;
        private const float AngularVelocityMagic = 25f;

        [HideInInspector]
        public Rigidbody Rigidbody;

        [HideInInspector]
        public NVRInteractableItem Item;

        public bool IsAttached;

        protected virtual void Awake()
        {
            IsAttached = false;

            Item = FindNVRItem(this.gameObject);
            if (Item == null)
            {
                Debug.LogError("No NVRInteractableItem found on this object. " + this.gameObject.name, this.gameObject);
            }

            AttachPointMapper.Register(this.GetComponent<Collider>(), this);
        }

        protected virtual void Start()
        {
            Rigidbody = Item.Rigidbody;
        }

        private NVRInteractableItem FindNVRItem(GameObject gameobject)
        {
            NVRInteractableItem item = gameobject.GetComponent<NVRInteractableItem>();

            if (item != null)
                return item;

            if (gameobject.transform.parent != null)
                return FindNVRItem(gameobject.transform.parent.gameObject);

            return null;
        }

        public virtual void Attached(NVRAttachJoint joint)
        {
            Vector3 targetPosition = joint.transform.position + (Item.transform.position - this.transform.position);
            Rigidbody.MovePosition(targetPosition);
            if (joint.MatchRotation == true)
            {
                Rigidbody.MoveRotation(joint.transform.rotation);
            }

            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero; 

            IsAttached = true;
            Rigidbody.useGravity = false;
        }
        public virtual void Detached(NVRAttachJoint joint)
        {
            IsAttached = false;

            if (Item.EnableGravityOnDetach == true)
            {
                Rigidbody.useGravity = true;
            }
        }

        public virtual void PullTowards(NVRAttachJoint joint)
        {
            float velocityMagic = VelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);
            float angularVelocityMagic = AngularVelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);

            Vector3 positionDelta = joint.transform.position - this.transform.position;
            Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.deltaTime;

            if (float.IsNaN(velocityTarget.x) == false)
            {
                velocityTarget = Vector3.MoveTowards(Item.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
                Item.AddExternalVelocity(velocityTarget);
            }


            if (joint.MatchRotation == true)
            {
                Quaternion rotationDelta = joint.transform.rotation * Quaternion.Inverse(Item.transform.rotation);

                float angle;
                Vector3 axis;

                rotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                if (angle != 0)
                {
                    Vector3 angularTarget = angle * axis;
                    if (float.IsNaN(angularTarget.x) == false)
                    {
                        angularTarget = (angularTarget * angularVelocityMagic) * Time.deltaTime;
                        angularTarget = Vector3.MoveTowards(Item.Rigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
                        Item.AddExternalAngularVelocity(angularTarget);
                    }
                }
            }
        }
    }
}