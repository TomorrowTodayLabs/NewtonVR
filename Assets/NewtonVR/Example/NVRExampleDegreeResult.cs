using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleDegreeResult : MonoBehaviour
    {
        [SerializeField] NVRInteractableItem Knob;

        private TextMesh Text;

        private void Awake()
        {
            Text = GetComponent<TextMesh>();
        }

        private void Update()
        {
            Text.text = ((int)Knob.transform.localEulerAngles.y).ToString();
        }
    }
}