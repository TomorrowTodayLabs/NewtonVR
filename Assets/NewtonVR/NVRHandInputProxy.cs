using UnityEngine;
using System.Collections;

using NewtonVR;

/// <summary>
/// Abstracts inputs used by NVRHand to help make it easier to provide different input sources
/// </summary>
public abstract class NVRHandInputProxy : MonoBehaviour {
    public abstract bool isReady();
    public abstract NVRButtonInputs getButtonState(NVRButtonID button);
}
