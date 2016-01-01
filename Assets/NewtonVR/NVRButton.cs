using UnityEngine;
using System.Collections;

public class NVRButton : MonoBehaviour
{
    public Rigidbody Rigidbody;

    public float PushedDistance = 0.075f;

    public bool ButtonDown = false;
    public bool ButtonUp = false;
    public bool ButtonIsPushed = false;
    public bool ButtonWasPushed = false;

    private Transform InitialPosition;
    private float MinDistance = 0.001f;

    private float PositionMagic = 1000f;

    private float CurrentDistance = -1;

    private void Awake()
    {
        InitialPosition = new GameObject("Initial Button Position").transform;
        InitialPosition.parent = this.transform.parent;
        InitialPosition.localPosition = Vector3.zero;
        InitialPosition.localRotation = Quaternion.identity;
        
        if (Rigidbody == null)
            Rigidbody = this.GetComponent<Rigidbody>();

        if (Rigidbody == null)
        {
            Debug.LogError("There is no rigidbody attached to this button.");
        }
    }

    private void FixedUpdate()
    {
        CurrentDistance = Vector3.Distance(this.transform.position, InitialPosition.position);

        if (CurrentDistance > MinDistance)
        {
            Vector3 PositionDelta = InitialPosition.position - this.transform.position;
            this.Rigidbody.velocity = PositionDelta * PositionMagic * Time.fixedDeltaTime;
        }
    }

    private void Update()
    { 
        ButtonWasPushed = ButtonIsPushed;
        ButtonIsPushed = CurrentDistance > PushedDistance;

        if (ButtonWasPushed == false && ButtonIsPushed == true)
            ButtonDown = true;
        else
            ButtonDown = false;

        if (ButtonWasPushed == true && ButtonIsPushed == false)
            ButtonUp = true;
        else
            ButtonUp = false;
    }
}
