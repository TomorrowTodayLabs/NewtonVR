using UnityEngine;

namespace NewtonVR
{
    public class NVRAttachPoint : MonoBehaviour
    {
        [HideInInspector]
        public Rigidbody Rigidbody;

        [HideInInspector]
        public NVRInteractableItem Item;

        public float PositionMagic = 10f;

        public bool IsAttached;

        protected virtual void Awake()
        {
            IsAttached = false;

            Item = FindNVRItem(gameObject);
            if (Item == null)
            {
                Debug.LogError("No NVRInteractableItem found on this object. " + gameObject.name, gameObject);
            }

            AttachPointMapper.Register(GetComponent<Collider>(), this);
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
            Vector3 TargetPosition = joint.transform.position + (Item.transform.position - transform.position);
            Rigidbody.MovePosition(TargetPosition);

            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero; 

            IsAttached = true;
            Rigidbody.useGravity = false;
        }
        public virtual void Detached(NVRAttachJoint joint)
        {
            IsAttached = false;
            Rigidbody.useGravity = true;
        }

        public virtual void PullTowards(Vector3 jointPosition)
        {
            Vector3 delta = jointPosition - transform.position;
            Rigidbody.AddForceAtPosition(delta * PositionMagic, transform.position, ForceMode.VelocityChange);
        }
    }
}