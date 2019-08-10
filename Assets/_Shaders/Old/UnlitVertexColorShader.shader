Shader "Unlit/VertexColor"
 {
     Properties
     {
        [Toggle(LINEAR_COLORSPACE)]
        _LinearColorSpace ("Linear Color Space", Float) = 0
     }
     SubShader
     {
         Tags{ "RenderType" = "Opaque" }
         LOD 100
 
         Pass
         {
             CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag
             #pragma shader_feature LINEAR_COLORSPACE

             struct appdata
             {
                 float4 vertex : POSITION;
                 float4 color : COLOR;
             };
 
             struct v2f
             {
                 float4 vertex : SV_POSITION;
                 float4 color : COLOR;
             };
 
             v2f vert(appdata v)
             {
                 v2f o;
                 o.vertex = UnityObjectToClipPos(v.vertex);
                 
                #ifdef LINEAR_COLORSPACE
                   o.color = pow(v.color,2.2);
                #else
                   o.color = v.color;
                #endif
                 return o;
             }
 
 
             float4 frag(v2f i) : SV_Target
             {
                 return i.color;
             }
             ENDCG
         }
     }
 }