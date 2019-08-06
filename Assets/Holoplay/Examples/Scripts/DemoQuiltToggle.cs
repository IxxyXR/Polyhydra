//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Demos {
	public class DemoQuiltToggle : MonoBehaviour {
		public LookingGlass.Holoplay holoplay;
		public Texture2D quiltToOverrideWith;
		public enum WhatToShow { SceneOnly, QuiltOnly, SceneOnQuilt};
		public WhatToShow whatToShow = WhatToShow.SceneOnly;
		private WhatToShow lastFrame;

		void Start() {
			holoplay.overrideQuilt = quiltToOverrideWith;
			holoplay.renderOverrideBehind = true;
			holoplay.background = new Color(0, 0, 1, 1);
		}

		void Update () {
			if (whatToShow != lastFrame) {
				if (whatToShow == WhatToShow.SceneOnly) {
					holoplay.renderOverrideBehind = true;
					holoplay.background = new Color(0, 0, 1, 1);
				}
				if (whatToShow == WhatToShow.QuiltOnly) {
					holoplay.renderOverrideBehind = false;
					holoplay.background = new Color(0, 0, 1, 1);
				}
				if (whatToShow == WhatToShow.SceneOnQuilt) {
					holoplay.renderOverrideBehind = true;
					holoplay.background = new Color(0, 0, 1, 0);
				}
				lastFrame = whatToShow;
			}
		}
	}
}
