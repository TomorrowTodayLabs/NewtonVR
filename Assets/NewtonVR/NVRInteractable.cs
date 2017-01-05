using UnityEngine;

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

        [Tooltip("If checked, this object can be picked up with two hands")]
        public bool TwoHanded = false;
        [HideInInspector]
        [Tooltip("This will be set to the second NVRHand that grabs the object.")]
        public NVRHand SecondHand = null;
        [HideInInspector]
        public Transform _trans;

        protected Collider[] Colliders;
        protected Vector3 ClosestHeldPoint;



        public virtual bool IsAttached
        {
            get
            {
                return AttachedHand != null;
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
            _trans = transform;  //cached to reduce GetComponent calls
        }

        protected virtual void Start()
        {
            UpdateColliders();
        }

        public virtual void ResetInteractable()
        {
            Awake();
            Start();
            AttachedHand = null;
        }

        public virtual void UpdateColliders()
        {
            Colliders = this.GetComponentsInChildren<Collider>();
            NVRInteractables.Register(this, Colliders);
        }

        protected virtual bool CheckForDrop()
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

            if (DropDistance != -1 && AttachedHand.CurrentInteractionStyle != InterationStyle.ByScript && shortestDistance > DropDistance)
            {
                DroppedBecauseOfDistance();
                return true;
            }

            return false;
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

        public virtual void HoveringUpdate(NVRHand hand, float forTime)
        {

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

        public virtual void BeginDualInteration(NVRHand hand)
        {
            SecondHand = hand;

        }

        public virtual void EndDualInteraction(NVRHand hand, bool forceFullDrop = false)
        {
            //remove hand and promote other hand to primary.
            NVRHand otherHand;
            if (AttachedHand == hand)
            {
                otherHand = SecondHand;
                AttachedHand = SecondHand;
                SecondHand = null;
            }
            else
            {
                otherHand = AttachedHand;
                SecondHand = null;
            }
            //reset PickupTransform to hand position and parent
            if (forceFullDrop)
            {
                otherHand.EndInteraction(this);  //might not need to call it on 'this', may want null
            }
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


        public virtual void AddExternalVelocity(Vector3 velocity)
        {
            Rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        }

        public virtual void AddExternalAngularVelocity(Vector3 angularVelocity)
        {
            Rigidbody.AddTorque(angularVelocity, ForceMode.VelocityChange);
        }

        protected virtual void OnDestroy()
        {
            ForceDetach();
            NVRInteractables.Deregister(this);
        }
    }
}
