using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleDegreeResult : MonoBehaviour
    {
        public NVRInteractableItem Knob;

        private TextMesh Text;

        private void Awake()
        {
            Text = this.GetComponent<TextMesh>();
        }

        private void Update()
        {
            Text.text = ((int)Knob.transform.localEulerAngles.y).ToString();
        }
    }
}