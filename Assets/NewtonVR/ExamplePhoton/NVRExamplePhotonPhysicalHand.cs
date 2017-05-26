using UnityEngine;
using System.Collections;
using NewtonVR;

public class NVRPhotonPhysicalHand : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        NVRInteractable interactable = NVRInteractables.GetInteractable(collision.collider);
        if (interactable != null)
        {
            NVRPhotonOwnership.PInstance.RequestOwnership(interactable);
        }
    }
}
