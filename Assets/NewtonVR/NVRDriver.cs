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


}
