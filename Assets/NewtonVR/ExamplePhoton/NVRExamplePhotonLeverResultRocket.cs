using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExamplePhotonLeverResultRocket : MonoBehaviour
    {
        public GameObject RocketPrefab;
        public NVRLever Control;

        private GameObject RocketInstance;
        
	    private void Awake()
        {
            if (PhotonNetwork.isMasterClient)
            {
                StartCoroutine(DoSpawnShip());
            }
        }
	
	    private void Update()
        {
            if (PhotonNetwork.isMasterClient)
            {
                if (Control.LeverEngaged == true)
                {
                    StartCoroutine(DoBlastOff());
                }
            }
	    }

        public IEnumerator DoBlastOff()
        {
            Rigidbody rb = RocketInstance.GetComponent<Rigidbody>();
            rb.AddRelativeForce(new Vector3(0, 1000, 0), ForceMode.Force);

            yield return new WaitForSeconds(0.5f);

            StartCoroutine(DoSpawnShip());
        }

        private IEnumerator DoSpawnShip()
        {
            RocketInstance = (GameObject)GameObject.Instantiate(RocketPrefab, this.transform.position, this.transform.rotation);
            RocketInstance.GetComponent<Rigidbody>().isKinematic = true;
            RocketInstance.GetComponent<NVRInteractableItem>().CanAttach = false; //warning: this won't propagate to other clients

            Vector3 startScale = Vector3.one * 0.1f;
            Vector3 endScale = Vector3.one;

            float startTime = Time.time;
            float overTime = 0.5f;
            float stopTime = startTime + overTime;

            while (Time.time < stopTime)
            {
                RocketInstance.transform.localScale = Vector3.Lerp(startScale, endScale, (Time.time - startTime) / overTime);
                yield return null;
            }

            RocketInstance.GetComponent<Rigidbody>().isKinematic = false;
            RocketInstance.GetComponent<NVRInteractableItem>().CanAttach = true;
        }
    }
}

