using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleGun : NVRInteractableItem
    {
        public GameObject BulletPrefab;

        public Transform FirePoint;

        public override void UseButtonUp()
        {
            base.UseButtonUp();

            GameObject bullet = GameObject.Instantiate(BulletPrefab);
            bullet.transform.position = FirePoint.position;
            bullet.transform.forward = this.transform.forward;

            bullet.GetComponent<Rigidbody>().AddRelativeForce(0, 0, 10000);
        }
    }
}