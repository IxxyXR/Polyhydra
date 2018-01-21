Shader "Unlit/ViewSpaceNormalsEdged" {
	Properties {
		_WireThickness ("Wire Thickness", RANGE(0, 800)) = 100
		_WireSmoothness ("Wire Smoothness", RANGE(0, 20)) = 3
		_WireColor ("Wire Color", Color) = (0.0, 1.0, 0.0, 1.0)
		_BaseColor ("Base Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_MaxTriSize ("Max Tri Size", RANGE(0, 200)) = 25
	}
        SubShader {
        
            Tags { 
                "RenderType"="Opaque"
            }
            
            Pass {
            
                Cull Off
            
                CGPROGRAM
                
                #pragma vertex vert
                #pragma fragment frag
                #pragma geometry geom      
                #include "UnityCG.cginc"

                uniform float _WireThickness = 100;
                uniform float _WireSmoothness = 3;
                uniform float4 _WireColor = float4(0.0, 1.0, 0.0, 1.0);
                uniform float4 _BaseColor = float4(0.0, 0.0, 0.0, 0.0);
                uniform float _MaxTriSize = 25.0;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
                
                struct v2g {
                    float4 projectionSpaceVertex : SV_POSITION;
                    half3 worldNormal : TEXCOORD0;
                    float4 worldSpacePosition : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                struct g2f {
                    float4 projectionSpaceVertex : SV_POSITION;
                    float4 worldSpacePosition : TEXCOORD0;
                    half3 worldNormal2 : TEXCOORD3;
                    float4 dist : TEXCOORD1;
                    float4 area : TEXCOORD2;
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                
                
                v2g vert(appdata v) {
                    v2g o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
                    o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
                    return o;
                }

                [maxvertexcount(3)]
                void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream) {
                
                    float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
                    float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
                    float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;
                
                    float2 edge0 = p2 - p1;
                    float2 edge1 = p2 - p0;
                    float2 edge2 = p1 - p0;
                
                    float4 worldEdge0 = i[0].worldSpacePosition - i[1].worldSpacePosition;
                    float4 worldEdge1 = i[1].worldSpacePosition - i[2].worldSpacePosition;
                    float4 worldEdge2 = i[0].worldSpacePosition - i[2].worldSpacePosition;
                    
                    float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
                    float wireThickness = 800 - _WireThickness;
                
                    g2f o;
                    
                    o.worldNormal2 = i[0].worldNormal;
                
                    o.area = float4(0, 0, 0, 0);
                    o.area.x = max(length(worldEdge0), max(length(worldEdge1), length(worldEdge2)));
                
                    o.worldSpacePosition = i[0].worldSpacePosition;
                    o.projectionSpaceVertex = i[0].projectionSpaceVertex;
                    o.dist.xyz = float3( (area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
                    o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[0], o);
                    triangleStream.Append(o);
                
                    o.worldSpacePosition = i[1].worldSpacePosition;
                    o.projectionSpaceVertex = i[1].projectionSpaceVertex;
                    o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
                    o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[1], o);
                    triangleStream.Append(o);
                
                    o.worldSpacePosition = i[2].worldSpacePosition;
                    o.projectionSpaceVertex = i[2].projectionSpaceVertex;
                    o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
                    o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[2], o);
                    triangleStream.Append(o);
                }
                
                fixed4 frag(g2f i) : SV_Target {
                    fixed4 face = 0;
                    face.rgb = mul(UNITY_MATRIX_V, i.worldNormal2/2)+.2;
                    float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];
                
                    // Early out if we know we are not on a line segment.
                    if(minDistanceToEdge > 0.9 || i.area.x > _MaxTriSize) {
                        return fixed4(face);
                    }
                
                    // Smooth our line out
                    float t = exp2(_WireSmoothness * -1.0 * minDistanceToEdge * minDistanceToEdge);
                    _WireColor = abs(1 - face);
                    fixed4 finalColor = lerp(face, _WireColor, t);
                    
                    return finalColor;
                }

                ENDCG
            }
        }
    }