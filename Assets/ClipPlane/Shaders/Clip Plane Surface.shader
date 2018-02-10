Shader "Hidden/Clip Plane/Surface"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
			Cull Off

            Stencil
            {
                Ref 0
                Comp NotEqual
                Pass Replace
                Fail Replace
				ZFail Replace
            }
            
            CGPROGRAM
            #pragma target 3.0
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

            uniform fixed4 _LightColor0;

            float4 _Color;

            struct v2f
            {
                float4 pos      : POSITION;
                float4 col      : COLOR;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float4 norm = mul(unity_ObjectToWorld, v.normal);
                float3 normalDirection = normalize(norm.xyz);
                float4 AmbientLight = UNITY_LIGHTMODEL_AMBIENT;
                float4 LightDirection = normalize(_WorldSpaceLightPos0);
                float4 DiffuseLight = saturate(dot(LightDirection, normalDirection))*_LightColor0;
                o.col = float4(AmbientLight + DiffuseLight);

                return o;
            }

            float4 frag(v2f i) : COLOR
            {
                return _Color * i.col;
            }

            ENDCG
        }
    }
}
