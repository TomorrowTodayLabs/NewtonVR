/**
 * Copyright (c) 2017 The Campfire Union Inc - All Rights Reserved.
 *
 * Unauthorized copying of this file, via any medium, is strictly prohibited.
 * This source code is proprietary and confidential.
 *
 * Email:   info@campfireunion.com
 * Website: https://www.campfireunion.com
 */

using UnityEngine;

namespace NewtonVR {

	/// <summary>
	/// Structure used for passing simple transformation data
	/// </summary>
	public class NVRAlignment {
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		public NVRAlignment (Transform transform) {
			position = transform.position;
			rotation = transform.rotation;
			scale = transform.localScale;
		}

		public NVRAlignment (Vector3 position, Quaternion rotation, Vector3 scale) {
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
		}

		public float Distance (Transform transform) {
			return (Distance (new NVRAlignment (transform.position, transform.rotation, transform.localScale)));
		}

		public float Distance (NVRAlignment alignment) {
			float distance = 0;
			distance += Vector3.Distance (position, alignment.position);
			distance += (Quaternion.Angle (rotation, alignment.rotation) / 90) * scale.x;

			return distance;
		}
	}
}