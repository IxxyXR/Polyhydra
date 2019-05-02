//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Demos {
	public class DemoCursorInteraction : MonoBehaviour {
		public GameObject holoplay;
		public LookingGlass.Cursor3D cursor;

		private Vector3 nextPosition = Vector3.back;

		void Update () {
			if (Input.GetMouseButtonDown(0)) {
				nextPosition = cursor.GetWorldPos();
			}
			holoplay.transform.position = Vector3.Slerp(holoplay.transform.position, nextPosition, 0.1f);
			holoplay.transform.LookAt(Vector3.zero);
		}
	}
}