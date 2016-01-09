using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRExampleLeverResultRocket : MonoBehaviour {

        public NVRLever Control;
        public Rigidbody RigidBody;
        private bool WaitingToBlastOff = false;

	    // Use this for initialization
	    void Awake () {
            if (RigidBody == null)
                RigidBody = this.GetComponent<Rigidbody>();
            RigidBody.isKinematic = true;
            RigidBody.useGravity = false;
        }
	
	    // Update is called once per frame
	    void Update () {
            if (Control.CurrentPositionOfLever == NVRLever.LeverPosition.Max && WaitingToBlastOff)
            {
                BlastOff();
                WaitingToBlastOff = false;
            }
	    }

        public void BlastOff()
        {
            RigidBody.isKinematic = false;
            RigidBody.useGravity = true;
            RigidBody.AddExplosionForce(50f, new Vector3(this.transform.position.x, this.transform.position.y - 1, this.transform.position.z), 3, 50f, ForceMode.Force);

        }
    }
}

