//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;
using UnityEditor;

namespace LookingGlass {
	public class ManualPreviewSettings : ScriptableObject {
		public bool manualPosition = true;
		public Vector2Int position = new Vector2Int(0, 0);
	}

	[CustomEditor(typeof(ManualPreviewSettings))]
	public class ManualPreviewSettingsEditor : Editor {

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			if (GUILayout.Button(Preview.togglePreviewShortcut)) {
				Preview.TogglePreview();
			}
		}
	}
}
