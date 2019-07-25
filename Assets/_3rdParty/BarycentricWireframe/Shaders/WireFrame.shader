Shader "Custom/Wire Frame"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Gain ("Gain", Float) = 1.5
		_Color ("Color", Color) = (1,1,1,1)
		_EdgeColor ("Edge Color", Color) = (0,0,0,1)
		 [Toggle] _RemoveDiag("Remove diagonals", Float) = 0.
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma glsl
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile __ _REMOVEDIAG_ON

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2g
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 bary : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Gain;
			float4 _Color;
			float4 _EdgeColor;

			v2g vert (appdata v)
			{
				v2g o;
				o.vertex = mul(unity_ObjectToWorld, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			[maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                float3 param = float3(0.0, 0.0, 0.0);
 
                #if _REMOVEDIAG_ON
                float EdgeA = length(IN[0].vertex - IN[1].vertex);
                float EdgeB = length(IN[1].vertex - IN[2].vertex);
                float EdgeC = length(IN[2].vertex - IN[0].vertex);
               
                if(EdgeA > EdgeB && EdgeA > EdgeC)
                    param.y = 1.0;
                else if (EdgeB > EdgeC && EdgeB > EdgeA)
                    param.x = 1.0;
                else
                    param.z = 1.0;
                #endif
 
                g2f o;
                o.vertex = mul(UNITY_MATRIX_VP, IN[0].vertex);
				o.uv = IN[0].uv;
                o.bary = float3(1.0, 0.0, 0.0) + param;
                triStream.Append(o);

                o.vertex = mul(UNITY_MATRIX_VP, IN[1].vertex);
                o.uv = IN[1].uv;
				o.bary = float3(0.0, 0.0, 1.0) + param;
                triStream.Append(o);
                
				o.vertex = mul(UNITY_MATRIX_VP, IN[2].vertex);
                o.uv = IN[2].uv;
				o.bary = float3(0.0, 1.0, 0.0) + param;
                triStream.Append(o);
            }

			float edgeFactor(float3 bary)
			{
				float3 d = fwidth(bary);
				float3 a3 = smoothstep(float3(0, 0, 0), _Gain * d, bary);
				return min(min(a3.x, a3.y), a3.z);
			}

			fixed4 frag (g2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				float t = edgeFactor(i.bary);

				return lerp(_EdgeColor, col, t);
			}
			ENDCG
		}
	}
}