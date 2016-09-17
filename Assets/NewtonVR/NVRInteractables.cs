using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRInteractables : MonoBehaviour
    {
        private static readonly IDictionary<Collider, NVRInteractable> ColliderMapping = new Dictionary<Collider, NVRInteractable>();
        private static readonly IDictionary<NVRInteractable, Collider[]> NVRInteractableMapping = new Dictionary<NVRInteractable, Collider[]>();

        public static void Register(NVRInteractable interactable, Collider[] colliders)
        {
            NVRInteractableMapping.Add(interactable, colliders);

            colliders.Iterate(a => ColliderMapping.Add(a, interactable));
        }

        public static void Deregister(NVRInteractable interactable)
        {
            NVRPlayer.DeregisterInteractable(interactable);

            ColliderMapping = ColliderMapping.Where(mapping => mapping.Value != interactable).ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
            NVRInteractableMapping.Remove(interactable);
        }

        public static NVRInteractable GetInteractable(Collider collider)
        {
            NVRInteractable interactable;
            ColliderMapping.TryGetValue(collider, out interactable);
            return interactable;
        }
    }
}