using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class NVRExampleTeleporter : MonoBehaviour
    {
        public Color LineColor;
        public float LineWidth = 0.02f;

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
                Line.SetColors(LineColor, LineColor);
            }

            Line.useWorldSpace = true;
        }

        private void LateUpdate()
        {
            Line.enabled = (Hand != null && Hand.Inputs[Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger].SingleAxis > 0.01f);

            if (Line.enabled == true)
            {
                Line.material.SetColor("_Color", LineColor);
                Line.SetColors(LineColor, LineColor);
                Line.SetWidth(LineWidth, LineWidth);

                RaycastHit hitInfo;
                bool hit = Physics.Raycast(this.transform.position, this.transform.forward, out hitInfo, 1000);
                Vector3 endPoint;

                if (hit == true)
                {
                    endPoint = hitInfo.point;

                    if (Hand.Inputs[Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger].PressDown == true)
                    {
                        NVRInteractable LHandInteractable = NVRPlayer.Instance.LeftHand.CurrentlyInteracting;
                        NVRInteractable RHandInteractable = NVRPlayer.Instance.RightHand.CurrentlyInteracting;


                        Vector3 offset = NVRPlayer.Instance.Head.transform.position - NVRPlayer.Instance.transform.position;
                        offset.y = 0;

                        NVRPlayer.Instance.transform.position = hitInfo.point - offset;
                        if (LHandInteractable != null)
                        {
                            LHandInteractable.transform.position = NVRPlayer.Instance.LeftHand.transform.position;
                        }
                        if (RHandInteractable != null)
                        {
                            RHandInteractable.transform.position = NVRPlayer.Instance.RightHand.transform.position;
                        }
                    }
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