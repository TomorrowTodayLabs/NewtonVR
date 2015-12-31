using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public class NVRPlayer : MonoBehaviour
    {
        public static NVRPlayer Instance;
        public bool PhysicalHands = false;

        public NVRHand[] Hands;

        private Dictionary<Collider, NVRHand> ColliderToHandMapping;

        private void Awake()
        {
            Instance = this;
            NVRInteractables.Initialize();

            ColliderToHandMapping = new Dictionary<Collider, NVRHand>();
        }

        private void Start()
        {
            for (int index = 0; index < Hands.Length; index++)
            {
                ColliderToHandMapping.Add(Hands[index].GetComponent<Collider>(), Hands[index]);
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