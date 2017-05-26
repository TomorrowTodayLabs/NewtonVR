using UnityEngine;
using System.Collections;
using NewtonVR.NetworkPhoton;

namespace NewtonVR.ExamplePhoton
{
    public class NVRExamplePhotonGrower : NVRPhotonInteractableItem
    {
        public override void InteractingUpdate(NVRHand hand)
        {
            base.InteractingUpdate(hand);

            if (IsMine())
            {
                if (hand.Inputs[NVRButtons.Touchpad].PressUp == true)
                {
                    Vector3 randomPoint = Random.insideUnitSphere;
                    randomPoint *= Colliders[0].bounds.extents.x;
                    randomPoint += Colliders[0].bounds.center;
                    randomPoint += Colliders[0].bounds.extents;

                    photonView.RPC("SpawnBuddy", PhotonTargets.All, randomPoint);
                }
            }

        }

        protected void SpawnBuddy(Vector3 point)
        {
            GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newCube.transform.parent = this.transform;
            newCube.transform.localScale = this.transform.localScale / 2;
            newCube.transform.position = point;

            base.UpdateColliders();
        }
    }
}