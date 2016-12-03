using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    [RequireComponent(typeof(NVRHand))]
    public class NVRExampleTeleporter : MonoBehaviour
    {
        public Color LineColor;
        public float LineWidth = 0.02f;

        private LineRenderer Line;

        private NVRHand Hand;

        private NVRPlayer Player;

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

        private void Start()
        {
            Player = Hand.Player;
        }

        private void LateUpdate()
        {
            Line.enabled = (Hand != null && Hand.Inputs[NVRButtons.Trigger].SingleAxis > 0.01f);

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

                    if (Hand.Inputs[NVRButtons.Trigger].PressDown == true)
                    {
                        NVRInteractable LHandInteractable = Player.LeftHand.CurrentlyInteracting;
                        NVRInteractable RHandInteractable = Player.RightHand.CurrentlyInteracting;


                        Vector3 offset = Player.Head.transform.position - Player.transform.position;
                        offset.y = 0;

                        Player.transform.position = hitInfo.point - offset;
                        if (LHandInteractable != null)
                        {
                            LHandInteractable.transform.position = Player.LeftHand.transform.position;
                        }
                        if (RHandInteractable != null)
                        {
                            RHandInteractable.transform.position =Player.RightHand.transform.position;
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