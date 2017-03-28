using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRInteractables : MonoBehaviour
    {
        private static Dictionary<Collider, NVRInteractable> ColliderMapping;
        private static Dictionary<NVRInteractable, Collider[]> NVRInteractableMapping;
        private static List<NVRInteractable> NVRInteractableList;

        private static bool Initialized = false;

        public static void Initialize()
        {
            ColliderMapping = new Dictionary<Collider, NVRInteractable>();
            NVRInteractableMapping = new Dictionary<NVRInteractable, Collider[]>();
            NVRInteractableList = new List<NVRInteractable>();

            Initialized = true;
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