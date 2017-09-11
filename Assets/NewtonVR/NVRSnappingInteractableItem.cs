using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewtonVR {

	public class NVRSnappingInteractableItem : NVRInteractableItem {
		public float snapToMeters = 0.05f;
		public float snapToDegrees = 45f;

		private bool snap = false;

		protected override void UpdateVelocities () {
			foreach (var hand in AttachedHands) {
				if (hand.UseButtonDown) {
					snap = !snap;
					break;
				}
			}

			if (!snap) {
				base.UpdateVelocities ();
				return;
			}

			Vector3 targetItemPosition;
			Quaternion targetItemRotation;

			Vector3 targetHandPosition;
			Quaternion targetHandRotation;

			GetTargetValues (out targetHandPosition, out targetHandRotation, out targetItemPosition, out targetItemRotation);

			float velocityMagic = VelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);
			float angularVelocityMagic = AngularVelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);

			Vector3 positionDelta;
			Quaternion rotationDelta;

			float angle;
			Vector3 axis;

			// Snap to nearest meters
			targetHandPosition.x = Mathf.Round (targetHandPosition.x / snapToMeters) * snapToMeters;
			targetHandPosition.y = Mathf.Round (targetHandPosition.y / snapToMeters) * snapToMeters;
			targetHandPosition.z = Mathf.Round (targetHandPosition.z / snapToMeters) * snapToMeters;

			positionDelta = (targetHandPosition - targetItemPosition);

			// Snap to nearest degrees
			var rounded = targetHandRotation.eulerAngles;
			rounded.x = Mathf.Round (rounded.x / snapToDegrees) * snapToDegrees;
			rounded.y = Mathf.Round (rounded.y / snapToDegrees) * snapToDegrees;
			rounded.z = Mathf.Round (rounded.z / snapToDegrees) * snapToDegrees;
			targetHandRotation.eulerAngles = rounded;

			rotationDelta = targetHandRotation * Quaternion.Inverse (targetItemRotation);

			Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.deltaTime;
			if (float.IsNaN (velocityTarget.x) == false) {
				this.Rigidbody.velocity = Vector3.MoveTowards (this.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
			}

			rotationDelta.ToAngleAxis (out angle, out axis);

			if (angle > 180)
				angle -= 360;

			if (angle != 0) {
				Vector3 angularTarget = angle * axis;
				if (float.IsNaN (angularTarget.x) == false) {
					angularTarget = (angularTarget * angularVelocityMagic) * Time.deltaTime;
					this.Rigidbody.angularVelocity = Vector3.MoveTowards (this.Rigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
				}
			}

			if (VelocityHistory != null) {
				CurrentVelocityHistoryStep++;
				if (CurrentVelocityHistoryStep >= VelocityHistory.Length) {
					CurrentVelocityHistoryStep = 0;
				}

				VelocityHistory[CurrentVelocityHistoryStep] = this.Rigidbody.velocity;
				AngularVelocityHistory[CurrentVelocityHistoryStep] = this.Rigidbody.angularVelocity;
			}
		}
	}
}