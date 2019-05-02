//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass {
	[ExecuteInEditMode]
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/SimpleDOF/")]
	public class SimpleDOF : MonoBehaviour {

		public Holoplay holoplay;
		[Header("Dof Curve")]
		public float start = -1.5f;
		public float dip = -0.5f;
		public float rise =  0.5f;
		public float end =  2.0f;
		[Header("Blur")]
		[Range(0f, 2f)] public float blurSize = 1.0f;
		public bool horizontalOnly = true;
		public bool testFocus;
		Material passdepthMat;
		Material boxBlurMat;
		Material finalpassMat;

		void OnEnable() {
			// check for Holoplay
			if (holoplay == null) {
				holoplay = GetComponentInParent<Holoplay>();
				if (holoplay == null) {
					enabled = false;
					Debug.LogWarning("[Holoplay] Simple DOF needs to be on a Holoplay capture's camera");
					return;
				}
			}
			passdepthMat = new Material(Shader.Find("Holoplay/DOF/Pass Depth"));
			boxBlurMat   = new Material(Shader.Find("Holoplay/DOF/Box Blur"));
			finalpassMat = new Material(Shader.Find("Holoplay/DOF/Final Pass"));
		}

		void Update() {
			// make sure the Holoplay is capturing depth
			holoplay.cam.depthTextureMode = DepthTextureMode.Depth;
			// passing shader vars
			Vector4 dofParams = new Vector4(start, dip, rise, end) * holoplay.size;
			dofParams = new Vector4(
				1.0f / (dofParams.x - dofParams.y),
				dofParams.y,
				dofParams.z,
				1.0f / (dofParams.w - dofParams.z)
			);
			boxBlurMat.SetVector("dofParams", dofParams);
			boxBlurMat.SetFloat("focalLength", holoplay.GetCamDistance());
			finalpassMat.SetInt("testFocus", testFocus ? 1 : 0);
			if (horizontalOnly)
				Shader.EnableKeyword("_HORIZONTAL_ONLY");
			else
				Shader.DisableKeyword("_HORIZONTAL_ONLY");
		}

		void OnDisable() {
			DestroyImmediate(passdepthMat);
			DestroyImmediate(boxBlurMat);
			DestroyImmediate(finalpassMat);
		}

		void OnRenderImage(RenderTexture src, RenderTexture dest) {
			// make the temporary pass rendertextures
			var fullres     = RenderTexture.GetTemporary(src.width, src.height, 0);
			var fullresDest = RenderTexture.GetTemporary(src.width, src.height, 0);
			var blur1 = RenderTexture.GetTemporary(src.width / 2, src.height / 2, 0);
			var blur2 = RenderTexture.GetTemporary(src.width / 3, src.height / 3, 0);
			var blur3 = RenderTexture.GetTemporary(src.width / 4, src.height / 4, 0);

			// passes: start with depth
			Graphics.Blit(src, fullres, passdepthMat);

			// blur 1
			boxBlurMat.SetInt("blurPassNum", 0);
			boxBlurMat.SetFloat("blurSize", blurSize * 2f);
			Graphics.Blit(fullres, blur1, boxBlurMat);

			// blur 2
			boxBlurMat.SetInt("blurPassNum", 1);
			boxBlurMat.SetFloat("blurSize", blurSize * 3f);
			Graphics.Blit(fullres, blur2, boxBlurMat);

			// blur 3
			boxBlurMat.SetInt("blurPassNum", 2);
			boxBlurMat.SetFloat("blurSize", blurSize * 4f);
			Graphics.Blit(fullres, blur3, boxBlurMat);

			// setting textures
			finalpassMat.SetTexture("blur1", blur1);
			finalpassMat.SetTexture("blur2", blur2);
			finalpassMat.SetTexture("blur3", blur3);

			// final blit for foreground
			Graphics.Blit(fullres, fullresDest, finalpassMat);
			Graphics.Blit(fullresDest, dest);

			// disposing of stuff
			RenderTexture.ReleaseTemporary(fullres);
			RenderTexture.ReleaseTemporary(fullresDest);
			RenderTexture.ReleaseTemporary(blur1);
			RenderTexture.ReleaseTemporary(blur2);
			RenderTexture.ReleaseTemporary(blur3);
		}
	}
}