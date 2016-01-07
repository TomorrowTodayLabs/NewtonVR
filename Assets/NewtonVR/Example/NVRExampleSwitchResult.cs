using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleSwitchResult : MonoBehaviour
    {
        public NVRSwitch Switch;

        private Light SpotLight;

        private void Awake()
        {
            SpotLight = this.GetComponent<Light>();
        }

        private void Update()
        {
            SpotLight.enabled = Switch.CurrentState;
        }
    }
}