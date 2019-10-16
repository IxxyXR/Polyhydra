Shader "Custom/RaymarchViz"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
			// TODO: In 2019 - change to shader_feature_local 
			#pragma shader_feature _ SDFr_VISUALIZE_STEPS SDFr_VISUALIZE_HEATMAP SDFr_VISUALIZE_DIST

            #pragma vertex vert_proc_quad
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            #include "SDFrProcedural.hlsl"
            #include "SDFrVolumeTex.hlsl"
			#include "SDFrUtilities.hlsl"

            #define MAX_STEPS 512
            #define EPSILON 0.003
            #define NORMAL_DELTA 0.03
            
            uniform float4x4 _PixelCoordToViewDirWS;
            
            Texture3D _VolumeATex;

            StructuredBuffer<SDFrVolumeData> _VolumeBuffer;
            float4 _Sphere;
            float4 _Box;
            
            float sdSphere( float3 p, float s )
            {
                return length(p)-s;
            }
            
            float sdBox( float3 p, float3 b )
            {
              float3 d = abs(p) - b;
              return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
            }
             
			// ISSUE: When stepping through a volume and not intersecting, it seams to keep a low step size.
			// Probably cos closest intersect is previous AABB?
			// Should maybe have an encompassing bounding box for scene - so if we hit that we early out!!!!
            float DistanceFunction( float3 rayPos, float3 rayOrigin, float3 rayEnd )
            {
                float d = DistanceFunctionTex3D( rayPos, rayOrigin, rayEnd, _VolumeBuffer[0], _VolumeATex );
                return d;
            }
			// Does no boundss checking - so assuming we hit something - but still checking against all primitives
			float DistanceFunctionFast(float3 rayPos)
			{
				float d = DistanceFunctionTex3DFast(rayPos, _VolumeBuffer[0], _VolumeATex);
				return d;
			}

			// Calculate the furthest safe ray start distance based on bounds of each element.
			float FurthestRayStartDistance(float3 rayOrigin, float3 rayEnd)
			{
				float d0 = DistanceToAABB(rayOrigin, rayEnd, _VolumeBuffer[0]);
				float d1 = DistanceToAABB(rayOrigin, rayEnd, _VolumeBuffer[1]);
				float d2 = DistanceToAABB(rayOrigin, rayEnd, _VolumeBuffer[2]);
				float d3 = DistanceToAABB(rayOrigin, rayEnd, _VolumeBuffer[3]);

				float d = min(d0, d1);
				d = min(d2, d);
				d = min(d3, d);
				return d;
			}

            half4 frag (Varyings_Proc input) : SV_Target
            {
                //ray origin
                float3 ro = _WorldSpaceCameraPos;
                //ray from camera to pixel coordinates in world space
                float3 rd = -normalize(mul(float3(input.positionCS.xy, 1.0), (float3x3)_PixelCoordToViewDirWS));                
                float3 re = ro + rd * _ProjectionParams.z;
            
				// Set starting distance to furthest safe distance to closest AABB ( originally was 0 ).
				// Otherwise number of steps is much higher as step size is based on first distance found within AABB to SDF.
				float dist = FurthestRayStartDistance(ro, re);
				int steps = 0;

				// Need to exit if max dist obtained elese empty pixels will use MAX_STEPS for nothing!
				while ( steps < MAX_STEPS && dist < _ProjectionParams.z )
                {
                    float3 rayPos = ro + rd * dist;
                                        
                    float d = DistanceFunction(rayPos,ro,re); 
                    
                    if ( d < EPSILON )
                    {
#ifdef SDFr_VISUALIZE_DIST
						return half4(0, 0, dist / 10.0, 1);
#endif

#ifdef SDFr_VISUALIZE_STEPS
						return half4(steps / (float)MAX_STEPS, 0, 0, 1);
#endif

#ifdef SDFr_VISUALIZE_HEATMAP
						// HeatMap - Green = minimal, Red = maximum number of steps
						float	stepf = steps / (float)MAX_STEPS;
						float	hue = lerp(0.33, 0.0, stepf);
						float3	rgb = HsvToRgb(float3(hue, 1, 1));
						return	half4(rgb, 1);
#endif

                        //fast normal
                        float3 nx = rayPos + float3(NORMAL_DELTA,0,0);
                        float3 ny = rayPos + float3(0,NORMAL_DELTA,0);
                        float3 nz = rayPos + float3(0,0,NORMAL_DELTA);

						// Can we not assume that any grad function is going to hit bounds and thus use a simpler method call?
/*
						float dx = DistanceFunction(nx,ro,re)-d;
						float dy = DistanceFunction(ny,ro,re)-d;
						float dz = DistanceFunction(nz,ro,re)-d;
*/
/*
						float dx = DistanceFunctionFast(nx) - d;
						float dy = DistanceFunctionFast(ny) - d;
						float dz = DistanceFunctionFast(nz) - d;
*/
						float halfDelta = NORMAL_DELTA * 0.5;
						float dx = DistanceFunctionFast(rayPos + float3(halfDelta, 0, 0)) - DistanceFunctionFast(rayPos - float3(halfDelta, 0, 0));
						float dy = DistanceFunctionFast(rayPos + float3(0, halfDelta, 0)) - DistanceFunctionFast(rayPos - float3(0, halfDelta, 0));
						float dz = DistanceFunctionFast(rayPos + float3(0, 0, halfDelta)) - DistanceFunctionFast(rayPos - float3(0, 0, halfDelta));

                        float3 normalWS = normalize(float3(dx,dy,dz));
                    
                        //TODO lighting 
                    
                        return half4(normalWS,1);
                    }
                    dist += d;
					steps++;
                }

#ifdef SDFr_VISUALIZE_DIST
				return half4(0, 0, dist / 10.0, 1);
#endif

#ifdef SDFr_VISUALIZE_STEPS
				return half4(steps / (float)MAX_STEPS, 0, 0, 1);
#endif
#ifdef SDFr_VISUALIZE_HEATMAP
				// HeatMap - Green = minimal, Red = maximum number of steps
				float	stepf = steps / (float)MAX_STEPS;
				float	hue = lerp(0.33, 0.0, stepf);
				float3	rgb = HsvToRgb(float3(hue, 1, 1));
				return	half4(rgb, 1);
#endif
				return half4(0.2,0.2,0.2,1);
            }
            ENDCG
        }
    }
}