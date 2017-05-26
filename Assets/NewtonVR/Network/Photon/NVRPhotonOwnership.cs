using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;
using System;

public class NVRPhotonOwnership : NVRNetworkOwnership
{
    public static NVRPhotonOwnership PInstance;

    //we cache all these off to reduce getcomponent calls
    public Dictionary<GameObject, PhotonView> PhotonViewsByGameobject = new Dictionary<GameObject, PhotonView>();
    public Dictionary<NVRInteractable, PhotonView> PhotonViewsByInteractable = new Dictionary<NVRInteractable, PhotonView>();
    public Dictionary<PhotonView, NVRInteractable> InteractablesByPhotonView = new Dictionary<PhotonView, NVRInteractable>();


    protected override void Awake()
    {
        base.Awake();
        PInstance = this;

        //interactables don't register until start, so we're cool
        NVRInteractables.OnInteractableRegistration += OnInteractableRegistration;
        NVRInteractables.OnInteractableDeregistration += OnInteractableDeregistration;
    }

    public PhotonView GetPhotonView(NVRInteractable interactable)
    {
        return PhotonViewsByInteractable[interactable];
    }
    public PhotonView GetPhotonView(GameObject gameobject)
    {
        return PhotonViewsByGameobject[gameobject];
    }
    public NVRInteractable GetInteractable(PhotonView photonView)
    {
        return InteractablesByPhotonView[photonView];
    }

    private void OnInteractableRegistration(NVRInteractable interactable, Collider[] colliders)
    {
        GameObject rigidbodyGameobject;
        if (interactable.Rigidbody != null)
        {
            rigidbodyGameobject = interactable.Rigidbody.gameObject;
        }
        else
        {
            rigidbodyGameobject = interactable.gameObject;
        }

        if (PhotonViewsByGameobject.ContainsKey(rigidbodyGameobject) == false)
        {
            PhotonView photonView = rigidbodyGameobject.GetComponent<PhotonView>();
            if (photonView == null)
            {
                Debug.LogError("This nvr interactable doesn't have a photonview attached to the watched rigidbody.");
            }

            PhotonViewsByGameobject.Add(rigidbodyGameobject, photonView);
            PhotonViewsByInteractable.Add(interactable, photonView);
        }
    }

    private void OnInteractableDeregistration(NVRInteractable interactable)
    {
        if (interactable != null)
        {
            GameObject rigidbodyGameobject;
            if (interactable.Rigidbody != null)
            {
                rigidbodyGameobject = interactable.Rigidbody.gameObject;
            }
            else
            {
                rigidbodyGameobject = interactable.gameObject;
            }

            PhotonViewsByGameobject.Remove(rigidbodyGameobject);
            PhotonViewsByInteractable.Remove(interactable);
        }
    }

    public override void RequestOwnership(NVRInteractable interactable)
    {
        PhotonView view = GetPhotonView(interactable);
        if (view != null)
        {
            if (view.isMine == false)
            {
                view.RequestOwnership();
            }
        }
    }

    public override bool IsMine(NVRInteractable interactable)
    {
        PhotonView view = GetPhotonView(interactable);
        if (view != null)
        {
            return view.isMine;
        }

        return false;
    }
}
