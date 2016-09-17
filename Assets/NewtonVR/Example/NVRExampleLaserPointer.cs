using UnityEngine;

namespace NewtonVR.Example
{
    public class NVRExampleLaserPointer : MonoBehaviour
    {
        [SerializeField] Color LineColor;
        [SerializeField] float LineWidth = 0.02f;
        [SerializeField] bool ForceLineVisible = true;
        [SerializeField] bool OnlyVisibleOnTrigger = true;

        private LineRenderer Line;

        private NVRHand Hand;

        private void Awake()
        {
            Line = GetComponent<LineRenderer>();
            Hand = GetComponent<NVRHand>();

            if (Line == null)
            {
                Line = gameObject.AddComponent<LineRenderer>();
            }

            if (Line.sharedMaterial == null)
            {
                Line.material = new Material(Shader.Find("Unlit/Color"));
                Line.material.SetColor("_Color", LineColor);
                Line.SetColors(LineColor, LineColor);
            }

            Line.useWorldSpace = true;
        }

        private void LateUpdate()
        {
            Line.enabled = ForceLineVisible || (OnlyVisibleOnTrigger && Hand != null && Hand.Inputs[Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger].IsPressed);

            if (Line.enabled)
            {
                Line.material.SetColor("_Color", LineColor);
                Line.SetColors(LineColor, LineColor);
                Line.SetWidth(LineWidth, LineWidth);

                RaycastHit hitInfo;
                bool hit = Physics.Raycast(transform.position, transform.forward, out hitInfo, 1000);
                Vector3 endPoint;

                if (hit)
                {
                    endPoint = hitInfo.point;
                }
                else
                {
                    endPoint = transform.position + (transform.forward * 1000f);
                }

                Line.SetPositions(new[] { transform.position, endPoint });
            }
        }
    }
}