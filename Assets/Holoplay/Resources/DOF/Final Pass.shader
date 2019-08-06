//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "Holoplay/DOF/Final Pass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D blur1;
			sampler2D blur2;
			sampler2D blur3;
			int testFocus;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample main tex
				float4 color = tex2D(_MainTex, i.uv);

				float4 c_blur1 = tex2D(blur1, i.uv);
				float4 c_blur2 = tex2D(blur2, i.uv);
				float4 c_blur3 = tex2D(blur3, i.uv);

				color = lerp(color, c_blur1, c_blur1.a);
				color = lerp(color, c_blur2, c_blur2.a);
				color = lerp(color, c_blur3, c_blur3.a);

				if (!testFocus)
				{
					color = float4(color.xyz, 1.0);
				}
				else
				{
					color = (c_blur1.a + c_blur2.a + c_blur3.a) * 0.333333333;
					color.a = 1.0;
				}

				return color;
			}
			ENDCG
		}
	}
}
