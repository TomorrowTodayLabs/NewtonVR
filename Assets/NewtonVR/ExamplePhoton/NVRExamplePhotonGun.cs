using UnityEngine;
using System.Collections;
using NewtonVR.NetworkPhoton;

namespace NewtonVR.PhotonExample
{
    public class NVRExamplePhotonGun : NVRPhotonInteractableItem
    {
        public GameObject BulletPrefab;

        public Transform FirePoint;

        public Vector3 BulletForce = new Vector3(0, 0, 250);

        public override void UseButtonDown()
        {
            base.UseButtonDown();

            if (IsMine())
            {
                GameObject bullet = PhotonNetwork.Instantiate(BulletPrefab.name, FirePoint.position, FirePoint.rotation, 0);

                bullet.GetComponent<Rigidbody>().AddRelativeForce(BulletForce);

                AttachedHand.TriggerHapticPulse(500);
            }
        }
    }
}