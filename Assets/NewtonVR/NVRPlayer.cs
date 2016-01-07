using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public class NVRPlayer : MonoBehaviour
    {
        public static NVRPlayer Instance;
        public bool PhysicalHands = false;

        public NVRHead Head;
        public NVRHand[] Hands;

        private Dictionary<Collider, NVRHand> ColliderToHandMapping;

        private void Awake()
        {
            Instance = this;
            NVRInteractables.Initialize();

            if (Head == null)
                this.GetComponentInChildren<NVRHead>();
            if (Hands == null || Hands.Length == 0)
                this.GetComponentsInChildren<NVRHand>();

            ColliderToHandMapping = new Dictionary<Collider, NVRHand>();
        }

        public void RegisterHand(NVRHand hand)
        {
            Collider[] colliders = hand.GetComponentsInChildren<Collider>();

            for (int index = 0; index < colliders.Length; index++)
            {
                if (ColliderToHandMapping.ContainsKey(colliders[index]) == false)
                {
                    ColliderToHandMapping.Add(colliders[index], hand);
                }
            }
        }

        public NVRHand GetHand(Collider collider)
        {
            return ColliderToHandMapping[collider];
        }

        public static void DeregisterInteractable(NVRInteractable interactable)
        {
            for (int index = 0; index < Instance.Hands.Length; index++)
            {
                Instance.Hands[index].DeregisterInteractable(interactable);
            }
        }
    }
}