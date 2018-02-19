Shader "Example/Slices" {
    Properties {
      _MainTex ("Texture", 2D) = "white" {}
      _BumpMap ("Bumpmap", 2D) = "bump" {}
      _Width ("Width", Range(0,20)) = 5
      _Amount ("Amount", Range(0,1)) = 0.5
      _Angle ("Angle", Range(0,1)) = 0.1
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      Cull Off
      CGPROGRAM
      #pragma surface surf Lambert
      struct Input {
          float2 uv_MainTex;
          float2 uv_BumpMap;
          float3 worldPos;
      };
      sampler2D _MainTex;
      sampler2D _BumpMap;
      float _Width;
      float _Amount;
      float _Angle;
      void surf (Input IN, inout SurfaceOutput o) {
          clip (frac((IN.worldPos.y+IN.worldPos.z*_Angle) * _Width) - _Amount);
          o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
          o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
      }
      ENDCG
    } 
    Fallback "Diffuse"
  }