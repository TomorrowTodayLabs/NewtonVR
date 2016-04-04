using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleGun : NVRInteractableItem
    {
        public GameObject BulletPrefab;

        public Transform FirePoint;

        public Vector3 BulletForce = new Vector3(0, 0, 500);

        public override void UseButtonDown()
        {
            base.UseButtonDown();

            GameObject bullet = GameObject.Instantiate(BulletPrefab);
            bullet.transform.position = FirePoint.position;
            bullet.transform.forward = this.transform.forward;

            bullet.GetComponent<Rigidbody>().AddRelativeForce(BulletForce);
        }
    }
}