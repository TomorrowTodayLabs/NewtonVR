using UnityEngine;

namespace NewtonVR
{
	public class NVRTeleportController : MonoBehaviour
	{
        public Transform BeamStart;

		private NVRTeleporter teleporter;

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
        
		private void Start()
        {
            teleporter = NVRPlayer.Instance.GetComponentInChildren<NVRTeleporter>();
            if (teleporter == null)
            {
                Debug.LogError("NVRTeleporter is Missing");
            }

            if (BeamStart == null)
                BeamStart = this.transform;

            controllerIndex = System.Convert.ToInt32(nvrHand.IsLeft);
		}

		private void FixedUpdate()
		{
			if (teleporter != null)
			{
				if (nvrHand.Inputs[teleporter.TeleportButton].IsPressed)
				{
					//Show Arc Teleport Preview
					validTeleportPosition = teleporter.UpdateArcTeleport(BeamStart, controllerIndex);
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

        private void OnDestroy()
        {
            if (teleporter != null)
            {
                teleporter.HideArcTeleport(controllerIndex);
            }
        }
    }
}
