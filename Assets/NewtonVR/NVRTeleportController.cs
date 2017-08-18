using UnityEngine;

namespace NewtonVR
{
	public class NVRTeleportController : MonoBehaviour
	{
		//NVRHand sometimes gets destroyed. Pull it each time.
		private NVRHand _nvrHand
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

		private int _controllerIndex = 0;
		private bool _held = false;

		private Vector3? _validTeleportPosition;

		private void Awake()
		{
			_controllerIndex = System.Convert.ToInt32(_nvrHand.IsLeft);
		}

		private void FixedUpdate()
		{
			if (NVRTeleporter.Instance != null)
			{
				if (_nvrHand.Inputs[NVRTeleporter.Instance.TeleportButton].IsPressed)
				{
					//Show Arc Teleport Preview
					_validTeleportPosition = NVRTeleporter.Instance.UpdateArcTeleport(transform, _controllerIndex);
					_held = true;
				}
				else if (_held == true)
				{
					//Was held on the last frame. Kill teleport preview
					NVRTeleporter.Instance.HideArcTeleport(_controllerIndex);
					_held = false;

					if (_validTeleportPosition != null)
					{
						NVRTeleporter.Instance.TeleportPlayer((Vector3)_validTeleportPosition);
					}
				}
			}
			else
			{
				Debug.LogWarning("NVRTeleporter is Missing");
			}
		}
	}
}
