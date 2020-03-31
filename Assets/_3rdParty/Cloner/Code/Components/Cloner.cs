using UnityEngine;
using System.Collections.Generic;

namespace Cloner
{
	public abstract class Cloner : InstanceRenderer
	{
		public bool update = true;
		public Mesh mesh;
		public Material material;
		public List<PointModifier> modifiers = new List<PointModifier> ();

		protected List<Matrix4x4> points = new List<Matrix4x4> ();

		protected abstract int PointCount { get; }
		protected abstract void CalculatePoints (ref List<Matrix4x4> points);

		private void Start ()
		{
			if (PointCount != points.Count)
				ResizePointsList (PointCount);
			UpdatePoints ();
		}
		private void Update ()
		{
			UpdateAll ();
		}

		public void UpdateAll ()
		{
			if (PointCount < 1)
				return;

			if (update)
			{
				if (PointCount != points.Count)
					ResizePointsList (PointCount);
				UpdatePoints ();
			}

			if (mesh != null && material != null)
				Draw (mesh, material, points);
		}

		public void UpdatePoints ()
		{
			if (mesh == null || material == null)
				return;
			CalculatePoints (ref points);
			for (int i = 0; i < modifiers.Count; i++)
			{
				var modifier = modifiers[i];
				if (modifier == null)
				{
					modifiers.RemoveAt (i);
					i--;
					continue;
				}
				points = modifiers[i].Modify (points);
			}
		}

		private void ResizePointsList (int goalCount)
		{
			var difference = goalCount - points.Count;

			if (difference > 0)
				for (int i = 0; i < difference; i++)
					points.Add (new Matrix4x4 ());
			else
				while (points.Count > goalCount)
					points.RemoveAt (points.Count - 1);
		}
	}
}