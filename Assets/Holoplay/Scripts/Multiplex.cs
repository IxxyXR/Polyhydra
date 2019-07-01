//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LookingGlass {
    [ExecuteInEditMode]
	[DisallowMultipleComponent]
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/Multiplex/")]
	public class Multiplex : MonoBehaviour {

		// variables
		public bool automaticArrangement;
		public int columns;
		public int rows;
		public float size = 5f;
		public float separation = 0.1f;
		public Holoplay[] holoplays;
		[Range(0f, 1f)] public float frustumShifting = 1f;
		private bool showUpdateWarning;
		bool initialSetup;
		// todo: possibly add more vars from the Holoplay for consistency

		// functions
		void OnEnable() {
			Plugin.PopulateLKGDisplays();
			// update warning
			int i;
			showUpdateWarning = false;
			for (i = 0; i < Plugin.CalibrationCount(); i++) {
				Calibration cal = new Calibration(i);
				// Debug.Log(cal.GetLKGName());
				if (cal.GetLKGName() == "LKG") {
					showUpdateWarning = true;
				}
			}
			initialSetup = true;
		}

		void Update() {
			Setup(initialSetup);
			initialSetup = false;
		}

		void OnGUI() {
			// don't show this if it's not playing or we don't need a warning
			if (!Application.isPlaying || !showUpdateWarning) 
				return;
			// save settings
			Color oldColor = GUI.color;
			// start drawing stuff
			int unitDiv = 20;
			int unit = Mathf.Min(Screen.width, Screen.height) / unitDiv;
			Rect rect = new Rect(unit, unit, unit*(unitDiv-2), unit*(unitDiv-2));
			GUI.color = Color.black;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			rect = new Rect(unit*2, unit*2, unit*(unitDiv-4), unit*(unitDiv-4));
			GUILayout.BeginArea(rect);
			var labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontSize = unit;
			GUI.color = new Color(.8f, .8f, .8f, 1f);
			GUILayout.Label("One or more Looking Glass devices need their calibration to be updated in order to properly run in multiplex mode.", labelStyle);
			GUILayout.Space(unit);
			GUI.color = new Color(.5f, .8f, .5f, 1f);
			GUILayout.Label("Click here to download the Calibration Update Tool:", labelStyle);
			var buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = unit; 
			GUI.color = new Color(.5f, 1f, .5f, 1f);
			if (GUILayout.Button("Download", buttonStyle)){
				// open url
				Application.OpenURL("https://github.com/Looking-Glass/Calibration-Update-Tool");
			}
			GUILayout.Space(unit);
			GUI.color = new Color(.5f, .5f, .5f, 1f);
			GUILayout.Label("Click here to hide this warning:", labelStyle);
			GUI.color = new Color(1f, .5f, .5f, 1f);
			if (GUILayout.Button("Hide Warning", buttonStyle)) {
				// hide warning
				showUpdateWarning = false;
			}
			GUILayout.EndArea();
			// restore settings
			GUI.color = oldColor;
		}

		void OnValidate() {
			// make sure size can't go negative
			// using this here instead of [Range()] attribute because size shouldn't need a slider
			size = Mathf.Clamp(size, 0.01f, Mathf.Infinity);
		}

		void Setup(bool reloadCalibration = false) {
			// if there's no holoplays, just return
			if (holoplays == null || holoplays.Length == 0) 
				return;
			// limit rows and columns to 8 units
			columns = Mathf.Clamp(columns, 1, 8);
			rows = Mathf.Clamp(rows, 1, 8 / columns);
			if (holoplays.Length > 8) {
				Debug.Log("[Holoplay] Multiplex cannot support more than 8 Holoplay Captures");
				System.Array.Resize(ref holoplays, 8);
			}
			// return if it's not automatic arrangement
			if (!automaticArrangement) return;
			// first sort displays
			List<DisplayPositioner> targetDisplayPositions = new List<DisplayPositioner>();
			for (int lkg = 0; lkg < Plugin.GetLKGcount(); lkg++) {
				targetDisplayPositions.Add(new DisplayPositioner () {
					targetLKG = lkg,
					targetDisplay = Plugin.GetLKGunityIndex(lkg),
					position = new Vector2Int(
						Mathf.RoundToInt(Plugin.GetLKGxpos(lkg) / 100f),
						Mathf.RoundToInt(Plugin.GetLKGypos(lkg) / 100f)
					)
				});
			}
			targetDisplayPositions.Sort();

			// automatic arrangement
			int i = 0;
			float totalCenterSweep = 0f;
			float horizontalOffsetSweep = 0f;
			float verticalOffsetSweep = 0f;
			if (columns > 1) {
				// hard coded magic number of 8 for now
				totalCenterSweep = 8f * Holoplay.Instance.cal.aspect * (columns - 1f) / Holoplay.Instance.cal.viewCone;
				horizontalOffsetSweep = 2f * (columns - 1f);
			}
			if (rows > 1)
				verticalOffsetSweep = -2f * (rows - 1f); // -Holoplay.Instance.fov * 0.5f * (rows - 1f);
			for (int x = 0; x < columns; x++) {
				for (int y = 0; y < rows; y++) {
					var h = holoplays[i];
					h.gameObject.SetActive(true);
					h.size = size;
					int yi = rows - 1 - y;
					float xOffset = x - (columns-1)*0.5f;
					float yOffset = yi - (rows-1)*0.5f;
					float span = size * (1f + separation);
					h.transform.localPosition = new Vector3(2f * xOffset * h.cal.aspect * span, 2f * yOffset * span);
					float offsetLerpX = columns > 1 ? ((float)x / (columns - 1f) - 0.5f) : 0f;
					float offsetLerpY = rows > 1 ? ((float)y / (rows - 1f) - 0.5f) : 0f;
					h.centerOffset = offsetLerpX * totalCenterSweep * frustumShifting;
					h.horizontalFrustumOffset = offsetLerpX * horizontalOffsetSweep * frustumShifting;
					h.verticalFrustumOffset = offsetLerpY * verticalOffsetSweep * frustumShifting;
					if (reloadCalibration) {
						if (targetDisplayPositions.Count > i) {
							h.targetDisplay = targetDisplayPositions[i].targetDisplay;
							h.targetLKG = targetDisplayPositions[i].targetLKG;
							h.ReloadCalibration ();
							if (Display.displays.Length > h.targetDisplay) {
								Display.displays[h.targetDisplay].Activate();
								Display.displays[h.targetDisplay].SetRenderingResolution(h.cal.screenWidth, h.cal.screenHeight);
							}
						} else {
							Debug.LogWarning("[Holoplay] Not enough displays connected for current multiview setup");
						}
					}
					i++;
				}
			}
			// disable the inactive ones
			while (i < 8) {
				holoplays[i].gameObject.SetActive(false);
				i++;
			}
		}

		public class DisplayPositioner : IComparable<DisplayPositioner> {
			public int targetLKG;
			public int targetDisplay;
			public Vector2Int position;
			public int CompareTo(DisplayPositioner other) {
				if (position.x < other.position.x) {
					return -1;
				} else if (position.x > other.position.x) {
					return 1;
				} else {
					if (position.y < other.position.y) {
						return -1;
					} else if (position.y > other.position.y) {
						return 1;
					}
				}
				return 0;
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Multiplex))]
	[CanEditMultipleObjects]
	public class MultiplexEditor : Editor {
		void OnEnable() {
			Plugin.PopulateLKGDisplays();
		}

		public override void OnInspectorGUI() {
#if UNITY_EDITOR_OSX
			EditorGUILayout.HelpBox("Multiplexing is currently not supported on macOS", MessageType.Error);
#endif
			DrawDefaultInspector();
		}
	}
#endif
}