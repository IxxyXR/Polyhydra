using Conway;
using UnityEngine;

public class ConwayAnimator : MonoBehaviour {
	
	public bool active = true;
	public float magnitude = 10;
	public float frequency = 1;
	public float offset = 0;
	public float updateFrequency = 0.03f;
	private PolyHydra poly;

	void Start()
	{
		poly = GetComponent<PolyHydra>();
		InvokeRepeating(nameof(Animate), 0, updateFrequency);
	}

	public void Animate()
	{
		if (active)
		{
			var op = poly.ConwayOperators[0];
			float amount = Mathf.Sin(Time.time * frequency) * magnitude + offset;
			amount = Mathf.Round(amount * 1000) / 1000f;
			op.amount = amount;
			poly.ConwayOperators[0] = op;
			poly.Rebuild();
		}
	}
}
