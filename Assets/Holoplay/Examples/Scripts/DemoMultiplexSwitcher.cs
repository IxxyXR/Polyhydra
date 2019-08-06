//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Demos {
	public class DemoMultiplexSwitcher : MonoBehaviour {
		public LookingGlass.Multiplex multiplexer;
		[Header("If you have one Looking Glass, tick this to preview the second display")]
		public bool swapDisplays;
		private bool lastToggleState = false;

		void Update () {
			if (swapDisplays != lastToggleState) {
				if (swapDisplays) {
					multiplexer.holoplays[0].targetDisplay = 2;
					multiplexer.holoplays[1].targetDisplay = 1;
				} else {
					multiplexer.holoplays[0].targetDisplay = 1;
					multiplexer.holoplays[1].targetDisplay = 2;
				}
				lastToggleState = swapDisplays;
			}
		}
	}
}
