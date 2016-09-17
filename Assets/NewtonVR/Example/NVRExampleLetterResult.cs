using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleLetterResult : MonoBehaviour
    {
        [SerializeField] NVRLetterSpinner LetterSpinner;

        private TextMesh Text;

        private void Awake()
        {
            Text = GetComponent<TextMesh>();
        }

        private void Update()
        {
            Text.text = LetterSpinner.GetLetter();
        }
    }
}