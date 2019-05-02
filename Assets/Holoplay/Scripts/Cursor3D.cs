//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass {
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/Cursor3D/")]
	public class Cursor3D : MonoBehaviour {

		private static Cursor3D instance;
		public static Cursor3D Instance {
			get {
				if (instance != null) return instance;
				instance = FindObjectOfType<Cursor3D>();
				return instance;
			}
		}
		Holoplay holoplay { get{ return Holoplay.Instance; } }
		[Tooltip("Disables the OS cursor at the start")]
		public bool disableSystemCursor = true;
		[Tooltip("Should the cursor scale follow the size of the Holoplay?")]
		public bool relativeScale = true;
		[System.NonSerialized] public Texture2D depthNormals;
		[System.NonSerialized] public Shader depthOnlyShader;
		[System.NonSerialized] public Shader readDepthPixelShader;
		[System.NonSerialized] public Material readDepthPixelMat;
		private bool frameRendered;
		private Camera cursorCam;

		private Vector3 worldPos;
		private Vector3 localPos;
		private Vector3 normal;
		private Quaternion rotation;
		private Quaternion localRotation;

		// returnable coordinates and normals
		public Vector3 GetWorldPos() { Update(); return worldPos; }
		public Vector3 GetLocalPos() { Update(); return localPos; }
		public Vector3 GetNormal() { Update(); return normal; }
		public Quaternion GetRotation() { Update(); return rotation; }
		public Quaternion GetLocalRotation() { Update(); return localRotation; }

		public RenderTexture debugTexture;

		void Start() {
			if (disableSystemCursor) Cursor.visible = false;
		}

		void OnEnable() {
			depthOnlyShader = Shader.Find("Holoplay/DepthOnly");
			readDepthPixelShader = Shader.Find("Holoplay/ReadDepthPixel");
			if (readDepthPixelShader != null) 
				readDepthPixelMat = new Material(readDepthPixelShader);
			depthNormals = new Texture2D( 1, 1, TextureFormat.ARGB32, false, true);
			cursorCam = new GameObject("cursorCam").AddComponent<Camera>();
			cursorCam.transform.SetParent(transform);
			cursorCam.gameObject.hideFlags = HideFlags.HideAndDontSave;
		}

		void OnDisable() {
			if (cursorCam.gameObject != null)
				DestroyImmediate(cursorCam.gameObject);
		}

		void Update() {
			if (holoplay == null) {
				Debug.LogWarning("[Holoplay] No holoplay detected for 3D cursor!");
				enabled = false;
				return;
			}
			if (frameRendered) return; // don't update if frame's been rendered already
			cursorCam.CopyFrom(holoplay.cam);
			var w = holoplay.quiltSettings.viewWidth;
			var h = holoplay.quiltSettings.viewHeight;
			var colorRT = RenderTexture.GetTemporary(
				w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1);
			colorRT.filterMode = FilterMode.Point; // important to avoid some weird edge cases
			var depthNormalsRT = RenderTexture.GetTemporary(
				1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			cursorCam.targetTexture = colorRT;
			float halfNormal = 0.5f;
			float midpointDist = holoplay.nearClipFactor / (holoplay.nearClipFactor + holoplay.farClipFactor);
			Color bgColor = new Color(halfNormal, halfNormal, midpointDist, midpointDist);
			cursorCam.backgroundColor = QualitySettings.activeColorSpace == ColorSpace.Gamma ? 
				bgColor : bgColor.gamma;	
			cursorCam.clearFlags = CameraClearFlags.SolidColor;
			cursorCam.cullingMask &= ~Physics.IgnoreRaycastLayer;
			cursorCam.RenderWithShader(depthOnlyShader, "RenderType");
				
			// copy single pixel and sample it
			// this keeps the ReadPixels from taking forever
			Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
			float monitorW = Screen.width;
			float monitorH = Screen.height;
			int activeDisplays = 0; // check if multiple displays are active
			foreach (var d in Display.displays) {
				if (d.active) activeDisplays++;	
			}
			if (Application.platform == RuntimePlatform.WindowsPlayer && activeDisplays > 1) {
				mousePos = Display.RelativeMouseAt(new Vector3(Input.mousePosition.x, Input.mousePosition.y));
				int monitor = Mathf.RoundToInt(mousePos.z);
				if (Display.displays.Length > monitor) {
					monitorW = Display.displays[monitor].renderingWidth;
					monitorH = Display.displays[monitor].renderingHeight;
				}
			}
			Vector2 mousePos01 = new Vector2(
				mousePos.x / monitorW,
				mousePos.y / monitorH);
			readDepthPixelMat.SetVector("samplePoint", new Vector4(mousePos01.x, mousePos01.y));
			Graphics.Blit(colorRT, depthNormalsRT, readDepthPixelMat);
			if (debugTexture == null)
				debugTexture = new RenderTexture(colorRT.width, colorRT.height, 0, colorRT.format, RenderTextureReadWrite.Linear);
			Graphics.Blit(colorRT, debugTexture);
			RenderTexture.active = depthNormalsRT;
			depthNormals.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
			depthNormals.Apply();
			Color enc = depthNormals.GetPixel(0, 0);
			// Debug.Log(enc.r + "r " + enc.g + "g");

			// find world pos from depth
			float depth = DecodeFloatRG(enc);
			// bool hit = depth >= 0.01f;
			// bool hit = true;
			// depth = hit ? depth : 0.5f; // if nothing hit, default depth
			depth = cursorCam.nearClipPlane + depth * (cursorCam.farClipPlane - cursorCam.nearClipPlane);
			Vector3 screenPoint = new Vector3(mousePos01.x, mousePos01.y, depth);
			worldPos = cursorCam.ViewportToWorldPoint(screenPoint);
			localPos = holoplay.transform.InverseTransformPoint(worldPos);
			if (isActiveAndEnabled)
				transform.position = worldPos;

			// find world normal based on view normal
			normal = DecodeViewNormalStereo(enc);
			// normals = hit ? normals : Vector3.forward; // if nothing hit, default normal
			normal = cursorCam.cameraToWorldMatrix * normal;
			rotation = Quaternion.LookRotation(-normal);
			localRotation = Quaternion.Inverse(holoplay.transform.rotation) * rotation;
			if (isActiveAndEnabled) {
				transform.rotation = rotation;
				// might as well set size here as well
				if (relativeScale) 
					transform.localScale = Vector3.one * holoplay.size * 0.1f;
			}

			// reset settings
			RenderTexture.ReleaseTemporary(colorRT);
			RenderTexture.ReleaseTemporary(depthNormalsRT);
			// set frame rendered
			frameRendered = true;
		}

		void LateUpdate() {
			frameRendered = false;
		}

		// copied from UnityCG.cginc
		Vector3 DecodeViewNormalStereo(Color enc4) {
			float kScale = 1.7777f;
			Vector3 enc4xyz = new Vector3(enc4.r, enc4.g, enc4.b);
			Vector3 asdf = Vector3.Scale(enc4xyz, new Vector3(2f*kScale, 2f*kScale, 0f));
			Vector3 nn = asdf + new Vector3(-kScale, -kScale, 1f);
			float g = 2.0f / Vector3.Dot(nn, nn);
			Vector2 nnxy = new Vector3(nn.x, nn.y) * g;
			Vector3 n = new Vector3(nnxy.x, nnxy.y, g - 1f);
			return n;
		}

		// copied from UnityCG.cginc
		float DecodeFloatRG(Color enc) {
			Vector2 encxy = new Vector2(enc.b, enc.a);
			Vector2 kDecodeDot = new Vector2(1.0f, 1.0f/255.0f);
			return Vector2.Dot(encxy, kDecodeDot);
		}
	}
}
