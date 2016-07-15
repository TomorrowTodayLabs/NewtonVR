using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using NewtonVR;

namespace NewtonVR
{
    public class NVRButtonUnityEvent : MonoBehaviour
    {
        [System.Serializable]
        public class ButtonPressedEvent : UnityEvent<NVRButtonUnityEvent> { };

        public ButtonPressedEvent OnPress;
        public NVRButton Button;
        public float RepeatPressDelay = 0.5f;
        float _repeatPressCountdown = 0f;

        void Awake()
        {
            if (Button == null) Button = GetComponent<NVRButton>();
        }

        void Start()
        {

        }

        void Update()
        {
            if (Button.ButtonDown && _repeatPressCountdown <= 0)
            {
                _repeatPressCountdown = RepeatPressDelay;
                OnPress.Invoke(this);
            }
            else
            {
                _repeatPressCountdown -= Time.deltaTime;
            }
        }
    }
}

