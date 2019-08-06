//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoButtonPresses : MonoBehaviour {

	[Header("Press physical buttons on Looking Glass for effect")]

	public bool squareIsCurrentlyDown;
	public bool leftIsCurrentlyDown;
	public bool rightIsCurrentlyDown;
	public bool circleIsCurrentlyDown;
	void Update () {
		if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.SQUARE)) { MakeNewSphereAt(new Vector3(-5.8f, -5, 0)); }
		if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.LEFT))   { MakeNewSphereAt(new Vector3(-3.2f, -5, 0)); }
		if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.RIGHT))  { MakeNewSphereAt(new Vector3(3.2f, -5, 0)); }
		if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.CIRCLE)) { MakeNewSphereAt(new Vector3(5.8f, -5, 0)); }

		squareIsCurrentlyDown = LookingGlass.ButtonManager.GetButton(LookingGlass.ButtonType.SQUARE);
		leftIsCurrentlyDown = LookingGlass.ButtonManager.GetButton(LookingGlass.ButtonType.LEFT);
		rightIsCurrentlyDown = LookingGlass.ButtonManager.GetButton(LookingGlass.ButtonType.RIGHT);
		circleIsCurrentlyDown = LookingGlass.ButtonManager.GetButton(LookingGlass.ButtonType.CIRCLE);
	}

	void MakeNewSphereAt(Vector3 instanceLocation) {
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		go.transform.position = instanceLocation + Random.insideUnitSphere * 0.25f;
		go.GetComponent<Renderer>().material.color = Random.ColorHSV();
		Rigidbody rb = go.AddComponent<Rigidbody>();
		rb.useGravity = true;
		rb.AddForce(Vector3.up * 12, ForceMode.Impulse);
	}
}
