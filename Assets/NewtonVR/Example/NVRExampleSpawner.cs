using UnityEngine;
using System.Collections;
using NewtonVR;

namespace NewtonVR.Example
{
    public class NVRExampleSpawner : MonoBehaviour
    {
        public NVRButton Button;

        public GameObject ToCopy;

        private void Update()
        {
            if (Button.ButtonDown)
            {
                GameObject newGo = GameObject.Instantiate(ToCopy);
                newGo.transform.position = this.transform.position + new Vector3(0, 1, 0);
                newGo.transform.localScale = ToCopy.transform.lossyScale;
            }
        }
    }
}