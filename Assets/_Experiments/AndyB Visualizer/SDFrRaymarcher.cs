using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SDFrRaymarcher : MonoBehaviour
{
    public Shader shader;

    public enum VizTypes
    {
	    Normal,
	    IntensitySteps,
	    HeatmapSteps,
	    Distance,
    }
    public VizTypes visualisation = VizTypes.Normal;

    public Vector3 Scaling;
    public Transform Pivot;
    //public Texture3D sdfTexture;
    public RenderTexture sdfRTexture;

    private CommandBuffer _cmd;
    private Material _material;
    private VolumeData[] _volumesData;
    private ComputeBuffer _volumes;

    private const int VolumeDataStride = 76;
    private struct VolumeData
    {
        public Matrix4x4 WorldToLocal;
        public Vector3 Extents;
    }

    bool CheckResources()
    {
        return shader != null && Pivot != null;
    }

    private void OnEnable()
    {
        _cmd = new CommandBuffer();
        _material = new Material(shader);
        _material.hideFlags = HideFlags.DontSave;
        _volumesData = new VolumeData[4];
        _volumes = new ComputeBuffer(4,VolumeDataStride);
        _volumes.SetData(_volumesData);
		OnSetKeywords();
    }

    private void OnDisable()
    {
        _cmd?.Dispose();
        if (_material != null)
        {
            DestroyImmediate(_material);
            _material = null;
        }
        _volumes?.Dispose();
    }

	void OnSetKeywords()
	{
		Shader.DisableKeyword("SDFr_VISUALIZE_STEPS");
		Shader.DisableKeyword("SDFr_VISUALIZE_HEATMAP");
		Shader.DisableKeyword("SDFr_VISUALIZE_DIST");

		switch( visualisation )
		{
			case VizTypes.IntensitySteps:  Shader.EnableKeyword("SDFr_VISUALIZE_STEPS"); break;
			case VizTypes.HeatmapSteps:	Shader.EnableKeyword("SDFr_VISUALIZE_HEATMAP"); break;
			case VizTypes.Distance:		Shader.EnableKeyword("SDFr_VISUALIZE_DIST"); break;
		}
	}

	// Editor call Only
	private void OnValidate()
	{
		OnSetKeywords();
	}

	void OnPostRender()
    {
        if (!CheckResources()) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        //NOTE kind of overkill for just 2 volumes... but keeps it together
        _volumesData[0].WorldToLocal = Pivot.worldToLocalMatrix;
        _volumesData[0].Extents = Scaling;

        _volumes.SetData(_volumesData);


        AVolumeUtils.SetupRaymarchingMatrix(cam.fieldOfView,cam.worldToCameraMatrix,new Vector2(cam.pixelWidth, cam.pixelHeight));

        _cmd.Clear();

        _cmd.SetGlobalVector("_BlitScaleBiasRt",new Vector4(1f,1f,0f,0f));
        _cmd.SetGlobalVector("_BlitScaleBias", new Vector4(1f, 1f, 0f, 0f));
        _cmd.SetGlobalBuffer("_VolumeBuffer", _volumes);
        _cmd.SetGlobalTexture("_VolumeATex", sdfRTexture);

        _cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Quads, 4, 1);
        Graphics.ExecuteCommandBuffer(_cmd);
    }


}
