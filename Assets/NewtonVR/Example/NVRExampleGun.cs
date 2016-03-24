using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleGun : NVRInteractableItem
    {
        public GameObject BulletPrefab;

        public Transform FirePoint;

        public Vector3 BulletForce = new Vector3(0, 0, 1000);

        public void FireBullet()
        {
            GameObject bullet = GameObject.Instantiate(BulletPrefab);
            bullet.transform.position = FirePoint.position;
            bullet.transform.forward = this.transform.forward;

            bullet.GetComponent<Rigidbody>().AddRelativeForce(BulletForce);
        }

        public override void InteractingUpdate(NVRHand hand)
        {
            base.InteractingUpdate(hand);

            if (hand.UseButtonDown == true)
            {
                FireBullet();
            }
        }
    }
}