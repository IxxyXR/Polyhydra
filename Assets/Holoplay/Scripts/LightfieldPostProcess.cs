//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;

namespace LookingGlass {
	[ExecuteInEditMode]
	public class LightfieldPostProcess : MonoBehaviour {

		public Holoplay holoplay;

		void OnRenderImage(RenderTexture src, RenderTexture dest) {
			Graphics.Blit(holoplay.quiltRT, dest, holoplay.lightfieldMat);
			// Graphics.Blit(Holoplay.quiltRT, dest);
		}
	}
}
