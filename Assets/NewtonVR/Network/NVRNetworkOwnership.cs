using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;
using System;

public abstract class NVRNetworkOwnership : MonoBehaviour
{
    public static NVRNetworkOwnership Instance;

    public bool AllowStealingInteraction = true;

    protected virtual void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.transform.root.gameObject);
    }

    public abstract void RequestOwnership(NVRInteractable interactable);

    public abstract bool IsMine(NVRInteractable interactable);
}
