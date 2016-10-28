using UnityEngine;
using NewtonVR;

/// <summary>
/// NVRDriver provides an abstraction layer between NVRHand, NVRHead, etc., and any device or code which
/// needs to control them. For example, you may want the NVRHands to follow the position and orientation of
/// the motion controllers from an HTC Vive. Rather than calling directly into the SteamVR SDK from NVRHand,
/// a Vive-specific driver can be written from this class which updates the NVRHands as necessary. This makes
/// it easier to replace the Vive controllers with another device if necessary in the future.
/// 
/// You are expected to update the position, orientation, button state, etc., of all connected hands and heads
/// inside your own Driver. Depending on what you're connecting your driver to, this may be as simple as just
/// setting the hand/head positions in FixedUpdate, for example.
/// </summary>
public abstract class NVRDriver : MonoBehaviour {

    public NVRPlayer Player;
    public NVRHead Head;
    public NVRHand[] Hands;

    // Left hand is Hand 0, Right hand is Hand 1
    public NVRHand LeftHand
    {
        get { return Hands != null && Hands.Length > 0 ? Hands[0] : null; }
    }
    public NVRHand RightHand
    {
        get { return Hands != null && Hands.Length > 1 ? Hands[1] : null; }
    }

    /// <summary>
    /// Event fired any time the poses for the hands or head are updated.
    /// You are expected to call this regularly to ensure the hands and any interactables
    /// currently held by the hands can update their poses/velocities correctly.
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

    /// <summary>
    /// Sets all button states on the given hand
    /// </summary>
    /// <param name="hand"></param>
    public abstract void SetButtonStates(NVRHand hand);

    // Triggers a haptic pulse in the given hand
    public abstract void TriggerHapticPulse(NVRHand hand, ushort durationMicroSec);
    public abstract void LongHapticPulse(NVRHand hand, float seconds);

    // Retrieves device-specific name for each component (e.g. name given by hardware-specific SDK)
    public abstract string GetDeviceName(NVRHand hand);
    public abstract string GetDeviceName(NVRHead head);

    protected virtual void Update()
    {
        foreach (var hand in Hands)
        {
            SetButtonStates(hand);
        }
    }
}
