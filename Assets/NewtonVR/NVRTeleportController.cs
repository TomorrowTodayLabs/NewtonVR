using UnityEngine;

namespace NewtonVR
{
	public class NVRTeleportController : MonoBehaviour
	{
		NVRTeleporter teleporter;

		//NVRHand sometimes gets destroyed. Pull it each time.
		private NVRHand nvrHand
		{
			get
			{
				NVRHand hand = GetComponent<NVRHand>();
				if (hand == null)
				{
					Debug.LogError("NVRHand is missing!");
				}
				return hand;
			}
		}

		private int controllerIndex = 0;
		private bool held = false;

		private Vector3? validTeleportPosition;

		private void Awake()
		{
			teleporter = transform.parent.GetComponentInChildren<NVRTeleporter>();
			if (teleporter == null)
			{
				Debug.LogError("NVRTeleporter is Missing");
			}
		}

		private void Start()
		{ 
			controllerIndex = System.Convert.ToInt32(nvrHand.IsLeft);
		}

		private void FixedUpdate()
		{
			if (teleporter != null)
			{
				if (nvrHand.Inputs[teleporter.TeleportButton].IsPressed)
				{
					//Show Arc Teleport Preview
					validTeleportPosition = teleporter.UpdateArcTeleport(transform, controllerIndex);
					held = true;
				}
				else if (held == true)
				{
					//Was held on the last frame. Kill teleport preview
					teleporter.HideArcTeleport(controllerIndex);
					held = false;

					if (validTeleportPosition != null)
					{
						teleporter.TeleportPlayer((Vector3)validTeleportPosition);
					}
				}
			}
		}
	}
}
