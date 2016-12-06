using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleLaserPointer : MonoBehaviour
    {
        public Color LineColor;
        public float LineWidth = 0.02f;
        public bool ForceLineVisible = true;

        public bool OnlyVisibleOnTrigger = true;

        private LineRenderer Line;

        private NVRHand Hand;

        private void Awake()
        {
            Line = this.GetComponent<LineRenderer>();
            Hand = this.GetComponent<NVRHand>();

            if (Line == null)
            {
                Line = this.gameObject.AddComponent<LineRenderer>();
            }

            if (Line.sharedMaterial == null)
            {
                Line.material = new Material(Shader.Find("Unlit/Color"));
                Line.material.SetColor("_Color", LineColor);
                NVRHelpers.LineRendererSetColor(Line, LineColor, LineColor);
            }

            Line.useWorldSpace = true;
        }

        private void LateUpdate()
        {
            Line.enabled = ForceLineVisible || (OnlyVisibleOnTrigger && Hand != null && Hand.Inputs[NVRButtons.Trigger].IsPressed);

            if (Line.enabled == true)
            {
                Line.material.SetColor("_Color", LineColor);
                NVRHelpers.LineRendererSetColor(Line, LineColor, LineColor);
                NVRHelpers.LineRendererSetWidth(Line, LineWidth, LineWidth);

                RaycastHit hitInfo;
                bool hit = Physics.Raycast(this.transform.position, this.transform.forward, out hitInfo, 1000);
                Vector3 endPoint;

                if (hit == true)
                {
                    endPoint = hitInfo.point;
                }
                else
                {
                    endPoint = this.transform.position + (this.transform.forward * 1000f);
                }

                Line.SetPositions(new Vector3[] { this.transform.position, endPoint });
            }
        }
    }
}