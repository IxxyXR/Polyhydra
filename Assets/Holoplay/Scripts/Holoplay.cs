//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LookingGlass {
    [ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/Holoplay/")]
	public class Holoplay : MonoBehaviour {

		// variables
		// singleton
		private static Holoplay instance;
		public static Holoplay Instance { 
			get{ 
				if (instance != null) return instance; 
				instance = FindObjectOfType<Holoplay>();
				return instance;
			} 
		}

		// info
		public static readonly Version version = new Version(1,0,0);
		public const string versionLabel = "";

		// camera
		public CameraClearFlags clearFlags = CameraClearFlags.Color;
		public Color background = Color.black;
		public LayerMask cullingMask = -1;
		public float size = 5f;
		public float depth;
		public RenderingPath renderingPath = RenderingPath.UsePlayerSettings;
		public bool occlusionCulling = true;
		public bool allowHDR = true;
		public bool allowMSAA = true;
#if UNITY_2017_3_OR_NEWER
		public bool allowDynamicResolution = false;
#endif
		public int targetDisplay;
		[System.NonSerialized] public int targetLKG;
		public bool preview2D = false;
		[Range(5f, 90f)] public float fov = 14f;
		[Range(0.01f, 5f)] public float nearClipFactor = 1.5f;
		[Range(0.01f, 40f)] public float farClipFactor = 4f;
		public bool scaleFollowsSize;
		[System.NonSerialized] public float centerOffset;
		[System.NonSerialized] public float horizontalFrustumOffset;
		[System.NonSerialized] public float verticalFrustumOffset;

		// quilt
		public Quilt.Preset quiltPreset = Quilt.Preset.Automatic;
		public Quilt.Settings quiltSettings {
			get { 
				if (quiltPreset == Quilt.Preset.Custom)
					return customQuiltSettings;	
				return Quilt.GetPreset(quiltPreset); 
			} 
		}
		public Quilt.Settings customQuiltSettings = Quilt.GetPreset(Quilt.Preset.HiRes);
		public KeyCode screenshot2DKey = KeyCode.F9;
        public KeyCode screenshotQuiltKey = KeyCode.F10;
		public Texture2D overrideQuilt;
		public bool renderOverrideBehind;
		public RenderTexture quiltRT;
		[System.NonSerialized] public Material lightfieldMat;

		// gizmo
		public Color frustumColor = new Color32(0, 255, 0, 255);
		public Color middlePlaneColor = new Color32(150, 50, 255, 255);
		public Color handleColor = new Color32(75, 100, 255, 255);
		public bool drawHandles = true;
		float[] cornerDists = new float[3];
		Vector3[] frustumCorners = new Vector3[12];

		// events
		[Tooltip("If you have any functions that rely on the calibration having been loaded " +
			"and the screen size having been set, let them trigger here")]
		public LoadEvent onHoloplayReady;
		[System.NonSerialized] public LoadResults loadResults;
		[Tooltip("Will fire before each individual view is rendered. " +
			"Passes [0, numViews), then fires once more passing numViews (in case cleanup is needed)")]
		public ViewRenderEvent onViewRender;
		[System.Serializable]
		public class ViewRenderEvent : UnityEvent<Holoplay, int> {};

		// not in inspector
		[System.NonSerialized] public Camera cam;
		[System.NonSerialized] public Camera lightfieldCam;
		const string lightfieldCamName = "lightfieldCam";
		[System.NonSerialized] public Calibration cal;
		bool debugInfo;

		// functions
		void OnEnable() {
			cam = GetComponent<Camera>();
			cam.hideFlags = HideFlags.HideInInspector;
			lightfieldMat = new Material(Shader.Find("Holoplay/Lightfield"));
			quiltRT = new RenderTexture(quiltSettings.quiltWidth, quiltSettings.quiltHeight, 0) {
				filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
			instance = this; // most recently enabled Capture set as instance
			// lightfield camera (only does blitting of the quilt into a lightfield)
			var lightfieldCamGO = new GameObject(lightfieldCamName);
			lightfieldCamGO.hideFlags = HideFlags.HideAndDontSave;
			lightfieldCamGO.transform.SetParent(transform);
			var lightfieldPost = lightfieldCamGO.AddComponent<LightfieldPostProcess>();
			lightfieldPost.holoplay = this;
			lightfieldCam = lightfieldCamGO.AddComponent<Camera>();
#if UNITY_2017_3_OR_NEWER
			lightfieldCam.allowDynamicResolution = false;
#endif
			lightfieldCam.allowHDR = false;
			lightfieldCam.allowMSAA = false;
			lightfieldCam.cullingMask = 0;
			lightfieldCam.clearFlags = CameraClearFlags.Nothing;
			// load calibration
			loadResults = ReloadCalibration();
			if (!loadResults.calibrationFound)
				Debug.Log("[Holoplay] Attempting to load calibration, but none found!");
			if (!loadResults.lkgDisplayFound)
				Debug.Log("[Holoplay] No LKG display detected");
			Screen.SetResolution(cal.screenWidth, cal.screenHeight, true);
			// call initialization event
			if (onHoloplayReady != null)
				onHoloplayReady.Invoke(loadResults);
		}

		void OnDisable() {
			if (lightfieldMat != null)
				DestroyImmediate(lightfieldMat);
			if (RenderTexture.active == quiltRT) 
				RenderTexture.active = null;
			if (quiltRT != null) 
				DestroyImmediate(quiltRT);
			if (lightfieldCam != null)
				DestroyImmediate(lightfieldCam.gameObject);
		}

        void Update() {
            // 2d screenshot input
            if (Input.GetKeyDown(screenshot2DKey)) {
                RenderTexture renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24); // allocate a temporary rt
                cam.targetTexture = renderTexture; // set it as the target render texture
                cam.Render(); // so that it can be rendered by camera
                TakeScreenShot(renderTexture); // save the render texture to png file
                cam.targetTexture = null; // reset the target texture to null
                RenderTexture.ReleaseTemporary(renderTexture); // don't forget to release the memory
            }
            // quilt screenshot input
            if (Input.GetKeyDown(screenshotQuiltKey)) {
				var previousPreset = quiltPreset;
				quiltPreset = Quilt.Preset.Standard;
				var previousPreviewSettings = preview2D;
				preview2D = false;
				// set up the quilt for taking screens
				var tempQuilt = RenderTexture.GetTemporary(quiltSettings.quiltWidth, quiltSettings.quiltHeight, 0);
				var previousQuilt = quiltRT;
				quiltRT = tempQuilt;
				LateUpdate(); // renders the lightfield
                TakeScreenShot(quiltRT);
				// return quilt to normal
				quiltPreset = previousPreset;
				quiltRT = previousQuilt;
				preview2D = previousPreviewSettings;
            }
			// debug info
			if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.F8))
				debugInfo = !debugInfo;
			if (Input.GetKeyDown(KeyCode.Escape))
				debugInfo = false;
        }

        void LateUpdate () {
			// pass the calibration values to lightfield material
			PassSettingsToMaterial();
			// set up camera
			var dist = ResetCamera();
			var aspect = (float)cal.screenWidth / cal.screenHeight;
			var centerViewMatrix = cam.worldToCameraMatrix;
			var centerProjMatrix = cam.projectionMatrix;
			depth = Mathf.Clamp(depth, -100f, 100f);
			cam.depth = lightfieldCam.depth = depth;
			cam.targetDisplay = lightfieldCam.targetDisplay = targetDisplay;
			// override quilt
			if(overrideQuilt) {
				Graphics.Blit(overrideQuilt, quiltRT);
				// if only rendering override, exit here
				if (!renderOverrideBehind) {
					cam.enabled = false;
					lightfieldCam.enabled = true;
					PassSettingsToMaterial();
					return;
				}
			}
			// if it's a 2D preview, exit here
			cam.enabled = preview2D;
			lightfieldCam.enabled = !preview2D;
			if (preview2D) {
				cam.targetDisplay = targetDisplay;
				return;
			}
			// else continue with the lightfield rendering
			// get viewcone sweep
			float viewConeSweep = -dist * Mathf.Tan(cal.viewCone * Mathf.Deg2Rad);
			// projection matrices must be modified in terms of focal plane size
			float projModifier = 1f / (size * cam.aspect);
			// fov trick to keep shadows from disappearing
			cam.fieldOfView = fov + cal.viewCone;
			// set shadow distance to start from holoplay center
			float shadowDist = QualitySettings.shadowDistance;
			QualitySettings.shadowDistance += GetCamDistance();
			// render the views
			for (int i = 0; i < quiltSettings.numViews; i++) {
				// onViewRender
				if (onViewRender != null)
					onViewRender.Invoke(this, i);
				// get view rt
				var viewRT = RenderTexture.GetTemporary(quiltSettings.viewWidth, quiltSettings.viewHeight, 24);
				cam.targetTexture = viewRT;
				cam.aspect = aspect;
				// move the camera
				var viewMatrix = centerViewMatrix;
				var projMatrix = centerProjMatrix;
				float currentViewLerp = 0f; // if numviews is 1, take center view
				if (quiltSettings.numViews > 1)
					currentViewLerp = (float)i / (quiltSettings.numViews - 1) - 0.5f;
				viewMatrix.m03 += currentViewLerp * viewConeSweep;
				projMatrix.m02 += currentViewLerp * viewConeSweep * projModifier;
				cam.worldToCameraMatrix = viewMatrix;
				cam.projectionMatrix = projMatrix;
				// render and copy the quilt
				cam.Render();
				// note: not using graphics.copytexture because it does not honor alpha
				// reverse view because Y is taken from the top
				int ri = quiltSettings.viewColumns * quiltSettings.viewRows - i - 1;
				int x = (i % quiltSettings.viewColumns) * quiltSettings.viewWidth;
				int y = (ri / quiltSettings.viewColumns) * quiltSettings.viewHeight;
				// again owing to the reverse Y
				Rect rtRect = new Rect(x, y + quiltSettings.paddingVertical, quiltSettings.viewWidth, quiltSettings.viewHeight);
				Graphics.SetRenderTarget(quiltRT);
				GL.PushMatrix();
				GL.LoadPixelMatrix(0, (int)quiltSettings.quiltWidth, (int)quiltSettings.quiltHeight, 0);
				Graphics.DrawTexture(rtRect, viewRT);
				GL.PopMatrix();
				// done copying to quilt, release view rt
				cam.targetTexture = null;
				RenderTexture.ReleaseTemporary(viewRT);
				// this helps 3D cursor ReadPixels faster
				GL.Flush();
				// move to next view
        	}
			// onViewRender final pass
			if (onViewRender != null)
				onViewRender.Invoke(this, quiltSettings.numViews);
			// reset to center view
			cam.worldToCameraMatrix = centerViewMatrix;
			cam.projectionMatrix = centerProjMatrix;
			// not really necessary, but keeps gizmo from looking messed up sometimes
			cam.aspect = aspect;
			// reset fov after fov trick
			cam.fieldOfView = fov;
			// reset shadow dist
			QualitySettings.shadowDistance = shadowDist;
        }

		void OnValidate() {
			// make sure size can't go negative
			// using this here instead of [Range()] attribute because size shouldn't need a slider
			size = Mathf.Clamp(size, 0.01f, Mathf.Infinity);
			// custom quilt settings
			if (quiltPreset == Quilt.Preset.Custom) {
				SetupQuilt();
			}
		}

		void OnGUI() {
			if (debugInfo) {
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
				GUI.color = new Color(.5f, .8f, .5f, 1f);
				GUILayout.Label("Holoplay SDK " + version.ToString() + versionLabel, labelStyle);
				GUILayout.Space(unit);
				GUI.color = loadResults.calibrationFound ? new Color(.5f, 1f, .5f) : new Color(1f, .5f, .5f);
				GUILayout.Label("calibration: " + (loadResults.calibrationFound ? "loaded" : "not found"), labelStyle);
				// todo: this is giving a false positive currently
				// GUILayout.Space(unit);
				// GUI.color = new Color(.5f, .5f, .5f, 1f);
				// GUILayout.Label("lkg display: " + (loadResults.lkgDisplayFound ? "found" : "not found"), labelStyle);
				GUILayout.EndArea();
				// restore settings
				GUI.color = oldColor;
			}
		}

// unity throws out our rendered view when focus is lost, so we must render a frame when it comes back
#if UNITY_EDITOR
		void OnApplicationFocus(bool hasFocus) {
			if (hasFocus && !Application.isPlaying && loadResults.attempted) {
				LateUpdate();
			}
		}
#endif

		public float ResetCamera() {
			// scale follows size
			if (scaleFollowsSize) {
				transform.localScale = Vector3.one * size;
			}
			// force it to render in perspective
			cam.orthographic = false;
			// set up the center view / proj matrix
			cam.fieldOfView = fov;
			// get distance
			float dist = GetCamDistance();
			// set near and far clip planes based on dist
			cam.nearClipPlane = Mathf.Max(dist - size * nearClipFactor, 0.1f);
			cam.farClipPlane = Mathf.Max(dist + size * farClipFactor, cam.nearClipPlane);
			// reset matrices, save center for later
			cam.ResetWorldToCameraMatrix();
			cam.ResetProjectionMatrix();
			var centerViewMatrix = cam.worldToCameraMatrix;
			var centerProjMatrix = cam.projectionMatrix;
			centerViewMatrix.m23 -= dist;
			// if we have offsets, handle them here
			if (horizontalFrustumOffset != 0f) {
				centerViewMatrix.m03 += horizontalFrustumOffset * size * cal.aspect;
				centerProjMatrix.m02 += horizontalFrustumOffset;
			}
			if (verticalFrustumOffset != 0f) {
				centerViewMatrix.m13 += verticalFrustumOffset * size;
				centerProjMatrix.m12 += verticalFrustumOffset;
			}
			cam.worldToCameraMatrix = centerViewMatrix;
			cam.projectionMatrix = centerProjMatrix;
			// set some of the camera properties from inspector
			cam.clearFlags = clearFlags;
			cam.backgroundColor = background;
			cam.cullingMask = cullingMask;
			cam.renderingPath = renderingPath;
			cam.useOcclusionCulling = occlusionCulling;
			cam.allowHDR = allowHDR;
			cam.allowMSAA = allowMSAA;
#if UNITY_2017_3_OR_NEWER
			cam.allowDynamicResolution = allowDynamicResolution;
#endif
			// return distance (since it is useful after the fact and we have it anyway)
			return dist;
		}

		void PassSettingsToMaterial() {
			// exit if the material doesn't exist
			if (lightfieldMat == null) return;
			// pass values
			lightfieldMat.SetFloat("pitch", cal.pitch);
			lightfieldMat.SetFloat("slope", cal.slope);
			lightfieldMat.SetFloat("center", cal.center + centerOffset);
			lightfieldMat.SetFloat("subpixelSize", 1f / (cal.screenWidth * 3f));
			lightfieldMat.SetVector("tile", new Vector4(
                quiltSettings.viewColumns,
                quiltSettings.viewRows,
                quiltSettings.numViews,
                quiltSettings.viewColumns * quiltSettings.viewRows
            ));
            lightfieldMat.SetVector("viewPortion", new Vector4(
                quiltSettings.viewPortionHorizontal,
                quiltSettings.viewPortionVertical
            ));
            lightfieldMat.SetVector("aspect", new Vector4(
                cal.aspect,
                // if it's the default aspect (-1), just use the same aspect as the screen
                quiltSettings.aspect < 0 ? cal.aspect : quiltSettings.aspect,
                quiltSettings.overscan ? 1 : 0
            ));
		}

		public LoadResults ReloadCalibration() {
			var results = Plugin.GetLoadResults(Plugin.PopulateLKGDisplays()); // loads calibration as well
			// create a calibration object. 
			// if we find that the target display matches a plugged in looking glass,
			// use matching calibration
			cal = new Calibration(Plugin.GetLKGcalIndex(0));
			if (results.calibrationFound && results.lkgDisplayFound) {
				for (int i = 0; i < Plugin.GetLKGcount(); i++) {
					if (targetDisplay == Plugin.GetLKGunityIndex(i)) {
						cal = new Calibration(Plugin.GetLKGcalIndex(i));
						targetLKG = i;
					}
				}
			}
			PassSettingsToMaterial();
			return results;
		}

		/// <summary>
		/// Returns the camera's distance from the center.
		/// Will be a positive number.
		/// </summary>
		public float GetCamDistance() {
			return size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
		}

		/// <summary>
		/// Will set up the quilt and the quilt rendertexture.
		/// Should be called after modifying custom quilt settings.
		/// </summary>
		public void SetupQuilt() {
			customQuiltSettings.Setup(); // even if not custom quilt, just set this up anyway
			if (quiltRT != null) DestroyImmediate(quiltRT);
			quiltRT = new RenderTexture(quiltSettings.quiltWidth, quiltSettings.quiltHeight, 0) {
				filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
			PassSettingsToMaterial();
		}

        // save screenshot as png file with the given rendertexture
        void TakeScreenShot(RenderTexture rt) {
            Texture2D screenShot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            Graphics.SetRenderTarget(rt); // same as RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            Graphics.SetRenderTarget(null);// same as RenderTexture.active = null;
            byte[] bytes = screenShot.EncodeToPNG();
            string filename = string.Format("{0}/screen_{1}x{2}_{3}.png",
											System.IO.Path.GetFullPath("."), rt.width, rt.height,
											System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
        }

		void OnDrawGizmos() {
			Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
				frustumColor.gamma : frustumColor;
			float focalDist = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
			cornerDists[0] = focalDist;
			cornerDists[1] = cam.nearClipPlane;
			cornerDists[2] = cam.farClipPlane;
			for (int i = 0; i < cornerDists.Length; i++) {
				float dist = cornerDists[i];
				int offset = i * 4;
                frustumCorners[offset+0] = cam.ViewportToWorldPoint(new Vector3(0, 0, dist));
                frustumCorners[offset+1] = cam.ViewportToWorldPoint(new Vector3(0, 1, dist));
                frustumCorners[offset+2] = cam.ViewportToWorldPoint(new Vector3(1, 1, dist));
                frustumCorners[offset+3] = cam.ViewportToWorldPoint(new Vector3(1, 0, dist));
				// draw each square
				for (int j = 0; j < 4; j++) {
					Vector3 start = frustumCorners[offset+j];
					Vector3 end = frustumCorners[offset+(j+1)%4];
					if (i > 0) {
						// draw a normal line for front and back
						Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
							frustumColor.gamma : frustumColor;
						Gizmos.DrawLine(start, end);
					} else {
						// draw a broken, target style frame for focal plane
						Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
							middlePlaneColor.gamma : middlePlaneColor;
						Gizmos.DrawLine(start, Vector3.Lerp(start, end, 0.333f));
						Gizmos.DrawLine(end, Vector3.Lerp(end, start, 0.333f));
					}
				}
			}
			// connect them
			for (int i = 0; i < 4; i++)
				Gizmos.DrawLine(frustumCorners[4+i], frustumCorners[8+i]);
		}
	}
}
