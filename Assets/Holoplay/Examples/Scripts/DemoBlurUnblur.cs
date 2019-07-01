//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LookingGlass.Demos {
    public class DemoBlurUnblur : MonoBehaviour {
        public LookingGlass.SimpleDOF simpleDOF;
        public UnityEngine.UI.Text label;
        public bool enableDOFBlur;

        void Update() {
            if (enableDOFBlur) {
                label.text = "DOF Blurring";
                simpleDOF.enabled = true;
            } else {
                label.text = "No DOF Blurring";
                simpleDOF.enabled = false;
            }
        }
    }
}
