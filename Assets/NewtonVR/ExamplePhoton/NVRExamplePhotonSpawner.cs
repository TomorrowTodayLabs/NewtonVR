using UnityEngine;
using System.Collections;
using NewtonVR;

namespace NewtonVR.Example
{
    public class NVRExamplePhotonSpawner : MonoBehaviour
    {
        public NVRButton Button;

        public GameObject ToCopy;

        private void Update()
        {
            if (Button.ButtonDown && PhotonNetwork.isMasterClient)
            {
                GameObject newGo = PhotonNetwork.Instantiate(ToCopy.name, this.transform.position + new Vector3(0, 1, 0), Quaternion.identity, 0);
                newGo.transform.localScale = ToCopy.transform.lossyScale;
            }
        }
    }
}