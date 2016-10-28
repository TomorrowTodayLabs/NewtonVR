using UnityEngine;
using System.Linq;
using System.Collections;
using NewtonVR;

public class NVRExampleAIDriver : NVRDriver {
    [Tooltip("Object to try to grab")]
    public NVRInteractable grasp_target;
    public float grasp_distance_threshold = 0.04f;

    private bool grasping = false;
    private Vector3 grasp_pos;
    private float wave_direction = 1.0f;

    void Awake()
    {
        // hands MUST be initialized before we can use them!
        foreach (var hand in Hands)
        {
            hand.DoInitialize();
        }
    }

    protected override void Update()
    {
        base.Update();

        // face our target
        Head.transform.LookAt(grasp_target.transform, Vector3.up);
    }

    // each frame update the hand position to either move toward and object to grab,
    // or to wave it up and down after grabbing it.
    void FixedUpdate()
    {
        if (grasp_target == null)
            return;

        // if we're not holding something, reach for our grasp_target
        if (LeftHand.CurrentlyInteracting == null)
        {
            // keep reaching for it until we're near the object's transform
            if (!LeftHand.IsHovering || Vector3.Distance(LeftHand.transform.position, grasp_target.transform.position) > grasp_distance_threshold)
            {
                grasping = false;
                LeftHand.transform.position = Vector3.Lerp(LeftHand.transform.position, grasp_target.transform.position, Time.deltaTime);
            }
            else if (LeftHand.IsHovering) // got it!
            {
                grasping = true;
                grasp_pos = LeftHand.transform.position;
            }
        }
        else // we grabbed something!
        {
            if (Vector3.Distance(grasp_pos, LeftHand.transform.position) > 0.2f)
            {
                wave_direction *= -1;
            }

            LeftHand.transform.position += Vector3.down * 0.01f * wave_direction;

            // toss object if it's not what we wanted (added here so it'll have at least some velocity)
            if (LeftHand.CurrentlyInteracting != grasp_target)
            {
                grasping = false;
            }
        }
    }

    public override NVRButtonInputs GetButtonState(NVRHand hand, NVRButtonID button)
    {
        NVRButtonInputs buttonState = new NVRButtonInputs();

        switch (button)
        {
            case NVRButtonID.HoldButton:
                if (grasping && !hand.Inputs[button].IsPressed) // first frame of grasp
                {
                    buttonState.Axis = Vector2.one;
                    buttonState.SingleAxis = 1;
                    buttonState.PressDown = true;
                    buttonState.PressUp = false;
                    buttonState.IsPressed = true;
                    buttonState.TouchDown = true;
                    buttonState.TouchUp = false;
                    buttonState.IsTouched = true;
                }
                else if (grasping && hand.Inputs[button].IsPressed) // we're continuing to grasp
                {
                    buttonState.Axis = Vector2.one;
                    buttonState.SingleAxis = 1;
                    buttonState.PressDown = false;
                    buttonState.PressUp = false;
                    buttonState.IsPressed = true;
                    buttonState.TouchDown = false;
                    buttonState.TouchUp = false;
                    buttonState.IsTouched = true;
                }
                else if (!grasping && !hand.Inputs[button].IsPressed) // not grasping at all
                {
                    buttonState.Axis = Vector2.zero;
                    buttonState.SingleAxis = 0;
                    buttonState.PressDown = false;
                    buttonState.PressUp = false;
                    buttonState.IsPressed = false;
                    buttonState.TouchDown = false;
                    buttonState.TouchUp = false;
                    buttonState.IsTouched = false;
                }
                else if (!grasping && hand.Inputs[button].IsPressed) // first frame of not grasping after a grasp
                {
                    buttonState.Axis = Vector2.zero;
                    buttonState.SingleAxis = 0;
                    buttonState.PressDown = false;
                    buttonState.PressUp = true;
                    buttonState.IsPressed = false;
                    buttonState.TouchDown = false;
                    buttonState.TouchUp = true;
                    buttonState.IsTouched = false;
                }

                break;
            case NVRButtonID.UseButton:
                buttonState.Axis = Vector2.zero;
                buttonState.SingleAxis = 0;
                buttonState.PressDown = false;
                buttonState.PressUp = false;
                buttonState.IsPressed = false;
                buttonState.TouchDown = false;
                buttonState.TouchUp = false;
                buttonState.IsTouched = false;
                break;
            default:
                break;
        }

        return buttonState;
    }

    public override void SetButtonStates(NVRHand hand)
    {
        foreach (NVRButtonID button in hand.Inputs.Keys.ToList())
        {
            hand.Inputs[button] = GetButtonState(hand, button);
        }
    }

    public override string GetDeviceName(NVRHand hand)
    {
        return "RoboHand";
    }

    public override string GetDeviceName(NVRHead head)
    {
        return "RoboHead";
    }

    public override void TriggerHapticPulse(NVRHand hand, ushort durationMicroSec)
    {
        StartCoroutine(DoHapticPulse((float)durationMicroSec / 100000.0f));
    }

    public override void LongHapticPulse(NVRHand hand, float seconds)
    {
        StartCoroutine(DoHapticPulse(seconds));
    }

    private IEnumerator DoHapticPulse(float seconds)
    {
        float t = seconds;
        Vector3 base_position = Head.transform.position;

        while (t > 0)
        {
            t -= Time.deltaTime;

            // shake the head to show use of haptic pulse
            Head.transform.position = base_position + Random.insideUnitSphere * 0.005f;
            yield return null; 
        }

        // reset to original position
        Head.transform.position = base_position;
    }

}
