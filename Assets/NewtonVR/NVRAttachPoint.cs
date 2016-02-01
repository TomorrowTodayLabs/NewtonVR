using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;

namespace NewtonVR
{
    public class NVRAttachPoint : MonoBehaviour
    {
        [HideInInspector]
        public Rigidbody Rigidbody;

        [HideInInspector]
        public NVRInteractableItem Item;

        protected float PositionMagic = 500f;
        protected float RotationMagic = 500f;

        public bool IsAttached;

        private void Awake()
        {
            IsAttached = false;

            Item = FindNVRItem(this.gameObject);
            if (Item == null)
            {
                Debug.LogError("No NVRInteractableItem found on this object. " + this.gameObject.name, this.gameObject);
            }

            AttachPointMapper.Register(this.GetComponent<Collider>(), this);
        }

        private void Start()
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

        public void Attached(NVRAttachJoint joint)
        {
            this.SetItemPosition(joint.transform.position);

            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            IsAttached = true;
            Rigidbody.useGravity = false;
        }
        public void Detached(NVRAttachJoint joint)
        {
            IsAttached = false;
            Rigidbody.useGravity = true;
        }

        public void SetItemPosition(Vector3 position)
        {
            Vector3 TargetPosition = position + (this.transform.position - Item.transform.position);
            Rigidbody.MovePosition(TargetPosition);
        }

        public void PullTowards(Vector3 position)
        {
            Vector3 delta = position - this.transform.position;

            Rigidbody.AddForceAtPosition(delta * PositionMagic, this.transform.position);
        }
    }
}