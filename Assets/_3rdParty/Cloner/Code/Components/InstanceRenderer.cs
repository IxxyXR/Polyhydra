using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cloner
{
	[ExecuteInEditMode]
	public abstract class InstanceRenderer : MonoBehaviour
	{
		public ShadowCastingMode castShadows = ShadowCastingMode.On;
		public bool receiveShadows = true;

		private List<List<Matrix4x4>> batches;


		public void Draw (Mesh mesh, Material material, List<Matrix4x4> matrices)
		{
			batches = Split (matrices, 1023);

			for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
			{
				for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
				{
					Graphics.DrawMeshInstanced (mesh, subMeshIndex, material, batches[batchIndex], null, castShadows, receiveShadows);
				}
			}
		}

		private List<List<T>> Split<T> (List<T> source, int size)
		{
			return source
				.Select ((x, i) => new { Index = i, Value = x })
				.GroupBy (x => x.Index / size)
				.Select (x => x.Select (v => v.Value).ToList ())
				.ToList ();
		}
	}
}