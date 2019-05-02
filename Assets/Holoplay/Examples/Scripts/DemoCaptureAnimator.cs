//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Demos {
	public class DemoCaptureAnimator : MonoBehaviour {
		public LookingGlass.Holoplay holoplay;
		public bool animatePosition;
		public bool animateScale;
		public bool animateRotation;
		public bool animateClippingPlane;
		public bool animateFOV;

		private Vector3 positionDirection = Vector3.up;

		private float scaleDirection = 1;

		private float twistAmount = 0;
		private float twistDirection = 1;

		private float clippingPlaneDirection = 1;
		private float FOVAnimationDirection = 1;

		void Update () {

			// Move the Holoplay Capture up and down
			if (animatePosition) {
				if (holoplay.transform.position.y > 2f) {
					positionDirection = Vector3.down;
				} else if (holoplay.transform.position.y < -2f) {
					positionDirection = Vector3.up;
				}
				holoplay.transform.Translate(positionDirection * 0.02f);
			}

			// Move the Holoplay Capture up and down
			if (animateScale) {
				if (holoplay.size > 7.5) {
					scaleDirection = -1;
				} else if (holoplay.size < 2.5f) {
					scaleDirection = 1;
				}
				holoplay.size += scaleDirection * 0.025f;
			}

			// Twist the Holoplay Capture back and forth
			if (animateRotation) {
				if (twistAmount > 20f) {
					twistDirection = -1;
				} else if (twistAmount < -20f) {
					twistDirection = 1;
				}
				twistAmount += twistDirection * 0.5f;
				holoplay.transform.rotation = Quaternion.Euler(0, twistAmount, 0);
			}

			// Animate the far clipping plane
			if (animateClippingPlane) {
				if (holoplay.farClipFactor > 1.5f) {
					clippingPlaneDirection = -1;
				} else if (holoplay.farClipFactor < 0.1f) {
					clippingPlaneDirection = 1;
				}
				holoplay.farClipFactor += clippingPlaneDirection * 0.01f;
			}

			// Bump FOV back and forth
			if (animateFOV) {
				if (holoplay.fov > 60) {
					FOVAnimationDirection = -1;
				} else if (holoplay.fov < 15) {
					FOVAnimationDirection = 1;
				}
				holoplay.fov += FOVAnimationDirection * 0.25f;
			}
		}
	}
}
