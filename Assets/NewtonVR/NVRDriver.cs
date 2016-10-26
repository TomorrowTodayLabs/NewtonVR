using UnityEngine;
using System.Collections;
using NewtonVR;

public abstract class NVRDriver : MonoBehaviour {

    public NVRPlayer Player;
    public NVRHead Head;
    public NVRHand LeftHand;
    public NVRHand RightHand;

    /// <summary>
    /// Event fired any time the poses for the hands or head are updated
    /// </summary>
    public delegate void NewPosesEvent();
    public NewPosesEvent OnNewPoses;

    /// <summary>
    /// Retrieves button state (pressed/up, etc) for whatever device the given hand is using
    /// </summary>
    /// <param name="hand">Which hand to look up button states for</param>
    /// <param name="button">Which button to check (dev is responsible for mapping NVRButtonID to their device's buttons)</param>
    /// <returns>Complete state of given button for the current frame</returns>
    public abstract NVRButtonInputs GetButtonState(NVRHand hand, NVRButtonID button);

    // Triggers a haptic pulse in the given hand
    public abstract void TriggerHapticPulse(NVRHand hand, ushort durationMicroSec);
    public abstract void LongHapticPulse(NVRHand hand, float seconds);

    public abstract string GetDeviceName(NVRHand hand);
    public abstract string GetDeviceName(NVRHead head);
}
