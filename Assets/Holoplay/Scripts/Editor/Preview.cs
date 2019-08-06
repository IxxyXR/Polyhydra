//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LookingGlass {
	public static class Preview {
		// vars
        static object gameViewSizesInstance;
        static BindingFlags bindingFlags = 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic;
        static int tabSize = 22 - 5; //this makes sense i promise
		static Type gameViewWindowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        static MethodInfo getGroup;
		static EditorWindow gameViewWindow;
#if UNITY_EDITOR_OSX
		static int windowInitialized = 2; // a countdown, sort of
		public const string togglePreviewShortcut = "Toggle Preview ⌘E";
#else
		static int windowInitialized = 0;
		public const string togglePreviewShortcut = "Toggle Preview Ctrl + E";
#endif
		public const string manualSettingsPath = "Assets/HoloplayPreviewSettings.asset";
		static ManualPreviewSettings manualPreviewSettings;

		// functions
		// used to subscribe to the scene change update
		[InitializeOnLoadMethod]
		static void InitPreview() {
			// recheck display callback
			EditorSceneManager.sceneOpened += RecheckDisplayTarget;
			// close open windows if there isn't a looking glass
			EditorApplication.update += CloseExtraHoloplayWindows;
		}

		// for when the user switches scenes, update preview if it's open
		static int recheckDelay;
		public static void RecheckDisplayTarget(Scene openScene, OpenSceneMode openSceneMode){
			recheckDelay = 1;
			EditorApplication.update += RecheckDisplayTargetDelayed;
		}

		// needs to be delayed because otherwise stuff isn't done setting up
		public static void RecheckDisplayTargetDelayed() {
			if (recheckDelay-- > 0) return;
			HandlePreview(false);
			EditorApplication.update -= RecheckDisplayTargetDelayed;
		}

		[MenuItem("Holoplay/Toggle Preview %e", false, 1)]	
		public static void TogglePreview() {
			// try to load from manual settings if available
			if (manualPreviewSettings == null) {
				manualPreviewSettings = AssetDatabase.LoadAssetAtPath<ManualPreviewSettings>(manualSettingsPath);
			}
			// handle the preview
			HandlePreview(true);
		}

		public static void HandlePreview(bool toggling = true) {
			// set standalone resolution
			Plugin.PopulateLKGDisplays();
			// calibration loading happens in populate displays
			// Calibration.LoadCalibrations();
			// close the window if its open
			var currentWindows = Resources.FindObjectsOfTypeAll(gameViewWindowType);
			bool windowWasOpen = false;
			foreach (EditorWindow w in currentWindows) {
				if (w.name == "Holoplay") {
					w.Close();
					windowWasOpen = true;
				} else {
					// to avoid ugliness, if there is a game window open
					// make sure it takes the same resolution
					SetResolution(w);
				}
			}
			if (toggling) {
				if (windowWasOpen) return;
			} else {
				if (!windowWasOpen) return;
			}
			/*
				logic for multiplexing
				- get number of holoplays in the scene
				- for the target display of each Holoplay
					- create a window on that display
					- just assign 
			 */
			var hps = GameObject.FindObjectsOfType<Holoplay>();
			// open up a preview in the looking glass even if no holoplays found
			if (hps.Length == 0) {
                SetupPreviewWindow(new Calibration(Plugin.GetLKGcalIndex(0)), 0, 0);
			}
			foreach (var hp in hps) {
				SetupPreviewWindow(hp.cal, hp.targetLKG, hp.targetDisplay);
			}
		}

		static void SetupPreviewWindow(Calibration cal, int targetLKG, int targetDisplay) {
            bool isMac = Application.platform == RuntimePlatform.OSXEditor;
			if (UnityEditor.PlayerSettings.defaultScreenWidth != cal.screenWidth)
				UnityEditor.PlayerSettings.defaultScreenWidth = cal.screenWidth;
			if (UnityEditor.PlayerSettings.defaultScreenHeight != cal.screenHeight)
				UnityEditor.PlayerSettings.defaultScreenHeight = cal.screenHeight;
			// otherwise create one
			gameViewWindow = (EditorWindow)EditorWindow.CreateInstance(gameViewWindowType);
			gameViewWindow.name = "Holoplay";
			if (!isMac) {
				var showModeType = typeof(Editor).Assembly.GetType("UnityEditor.ShowMode");
				var showWithModeInfo = gameViewWindowType.GetMethod("ShowWithMode", bindingFlags);
				showWithModeInfo.Invoke(gameViewWindow, new [] { Enum.ToObject(showModeType, 1) });
			} else {
				if (windowInitialized == 2) {
					EditorApplication.update += UpdateWindowPos;
					windowInitialized = 1;
				}
				gameViewWindow = EditorWindow.GetWindow(gameViewWindowType);
			}
			// set window size and position
			gameViewWindow.maxSize = new Vector2(cal.screenWidth, cal.screenHeight + tabSize);
			gameViewWindow.minSize = gameViewWindow.maxSize;
			int xpos = Plugin.GetLKGxpos(targetLKG);
			int ypos = Plugin.GetLKGypos(targetLKG);
			// Debug.Log("targetLKG:" + targetLKG + " x:" + xpos + " y:" + ypos);
			if (manualPreviewSettings != null && manualPreviewSettings.manualPosition) {
				xpos = manualPreviewSettings.position.x;
				ypos = manualPreviewSettings.position.y;
			}
			gameViewWindow.position = new Rect(
				xpos, ypos - tabSize, gameViewWindow.maxSize.x, gameViewWindow.maxSize.y);
			// set the zoom and resolution
			SetZoom(gameViewWindow);
			SetResolution(gameViewWindow);
			// set display number
			var displayNum = gameViewWindowType.GetField("m_TargetDisplay", bindingFlags);
			displayNum.SetValue(gameViewWindow, targetDisplay);
		}

		static void CloseExtraHoloplayWindows() {
			var currentWindows = Resources.FindObjectsOfTypeAll(gameViewWindowType);
			Plugin.PopulateLKGDisplays();
			if (Plugin.GetLKGcount() < 1) {
				foreach (EditorWindow w in currentWindows) {
					if (w.name == "Holoplay") {
						w.Close();
						Debug.Log("[Holoplay] Closing extra Holoplay window");
					}
				}
			}
			EditorApplication.update -= CloseExtraHoloplayWindows;
		}

		// this won't work for multiple monitors
		// but multi-display doesn't work on mac anyway
		static void UpdateWindowPos() {
			if (windowInitialized > 0) {
				windowInitialized--;
			} else {
				int xpos = Plugin.GetLKGxpos(0);
				int ypos = Plugin.GetLKGypos(0);
				if (manualPreviewSettings != null && manualPreviewSettings.manualPosition) {
					xpos = manualPreviewSettings.position.x;
					ypos = manualPreviewSettings.position.y;
				}
				gameViewWindow.position = new Rect(
					xpos, ypos - tabSize + 5, // plus 5, don't know why, works
					gameViewWindow.maxSize.x, gameViewWindow.maxSize.y);
				EditorApplication.update -= UpdateWindowPos;
			}
		}

		static void SetZoom(EditorWindow gameViewWindow) {
            float targetScale = 1;
            var areaField = gameViewWindowType.GetField("m_ZoomArea", bindingFlags);
            var areaObj = areaField.GetValue(gameViewWindow);
            var scaleField = areaObj.GetType().GetField("m_Scale", bindingFlags);
            scaleField.SetValue(areaObj, new Vector2(targetScale, targetScale));
		}

		static void SetResolution(EditorWindow gameViewWindow) {
            PropertyInfo selectedSizeIndexProp = gameViewWindowType.GetProperty (
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            Type sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            getGroup = sizesType.GetMethod("GetGroup");
            gameViewSizesInstance = instanceProp.GetValue(null, null);
            var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
            var currentGroupType = (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
            var group = getGroup.Invoke(gameViewSizesInstance, new object[] { (int)currentGroupType });

            var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
            var displayTexts = (string[])getDisplayTexts.Invoke(group, null);
            int index = 0;
            for (int i = 0; i < displayTexts.Length; i++) {
                if (displayTexts[i].Contains("Standalone")) {
                    index = i;
                    break;
                }
            }
            if (index == 0) {
                Debug.LogWarning("[Holoplay] couldn't find standalone resolution in preview window");
            }
            selectedSizeIndexProp.SetValue(gameViewWindow, index, null);
		}

		[MenuItem("Assets/Create/Holoplay/Manual Preview Settings")]
		static void CreateManualPreviewAsset() {
			ManualPreviewSettings previewSettings = AssetDatabase.LoadAssetAtPath<ManualPreviewSettings>(manualSettingsPath);
			if (previewSettings == null) {
				previewSettings = ScriptableObject.CreateInstance<ManualPreviewSettings>();
				AssetDatabase.CreateAsset(previewSettings, manualSettingsPath);
				AssetDatabase.SaveAssets();
			}
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = previewSettings;
		}
	}
}
