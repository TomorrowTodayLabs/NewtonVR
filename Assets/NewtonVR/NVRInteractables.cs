using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NewtonVR
{
    public class NVRInteractables : MonoBehaviour
    {
        private static Dictionary<Collider, NVRInteractable> ColliderMapping;
        private static Dictionary<NVRInteractable, Collider[]> NVRInteractableMapping;
        private static List<NVRInteractable> NVRInteractableList;

        private static bool Initialized = false;

        public delegate void NVROnInteractableRegistration(NVRInteractable interactable, Collider[] colliders);
        public delegate void NVROnInteractableDeregistration(NVRInteractable interactable);

        public static event NVROnInteractableRegistration OnInteractableRegistration;
        public static event NVROnInteractableDeregistration OnInteractableDeregistration;

        private void Awake()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (Initialized == false)
            {
                ColliderMapping = new Dictionary<Collider, NVRInteractable>();
                NVRInteractableMapping = new Dictionary<NVRInteractable, Collider[]>();
                NVRInteractableList = new List<NVRInteractable>();

                Initialized = true;
            }
        }

        public static void Register(NVRInteractable interactable, Collider[] colliders)
        {
            if (Initialized == false)
            {
                Debug.LogError("[NewtonVR] Error: NVRInteractables.Register called before initialization.");
            }

            NVRInteractableMapping[interactable] = colliders;

            for (int index = 0; index < colliders.Length; index++)
            {
                ColliderMapping[colliders[index]] = interactable;
            }

            if (NVRInteractableList.Contains(interactable) == false)
            {
                NVRInteractableList.Add(interactable);
            }

            if (OnInteractableRegistration != null)
            {
                OnInteractableRegistration.Invoke(interactable, colliders);
            }
        }

        public static void Deregister(NVRInteractable interactable)
        {
            if (Initialized == false)
            {
                Debug.LogError("[NewtonVR] Error: NVRInteractables.Register called before initialization.");
            }

            NVRPlayer.DeregisterInteractable(interactable);

            ColliderMapping = ColliderMapping.Where(mapping => mapping.Value != interactable).ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
            NVRInteractableMapping.Remove(interactable);

            NVRInteractableList.Remove(interactable);

            if (OnInteractableDeregistration != null)
            {
                OnInteractableDeregistration.Invoke(interactable);
            }
        }

        public static NVRInteractable GetInteractable(Collider collider)
        {
            if (Initialized == false)
            {
                Debug.LogError("[NewtonVR] Error: NVRInteractables.Register called before initialization.");
            }

            NVRInteractable interactable;
            ColliderMapping.TryGetValue(collider, out interactable);
            return interactable;
        }

        public static List<NVRInteractable> GetAllInteractables()
        {
            return NVRInteractableList;
        }
    }
}