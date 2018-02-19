using UnityEngine;

public class RotateObject : MonoBehaviour {
	
	public float x = 1;
	public float y = 1;
	public float z = 0.1f;
	public bool active = true;
	
	// Update is called once per frame
	void Update () {
		if (active) {gameObject.transform.Rotate(x, y, z);}
	}

	public void Randomize() {
		x = -1 + (Random.value + Random.value);
		y = -1 + (Random.value + Random.value);
		z = -1 + (Random.value + Random.value);
	}

	public void Pause() {
		active = !active;
	}
}
