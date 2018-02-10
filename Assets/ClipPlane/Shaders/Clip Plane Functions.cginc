#ifndef __CLIP_PLANE_FUNCTIONS__
#define __CLIP_PLANE_FUNCTIONS__
#include "UnityCG.cginc"

// Returns a value indicating whether or or not the fragment is on the
// "clipped" side of the clip plane
// planeVector.xyz: The normal direction to clip
// planeVector.w:   The distance along the vector to clip
// worldPos:		The fragment position in world space
// useWorldSpace:	Whether or not to treat the plane as though its in world space. 1 for true, 0 for falase
float isClipped(float4 planeVector, float3 worldPos, float useWorldSpace) {
	// Calculate clip value
	float3 wp = worldPos;
	float3 pnorm = normalize(planeVector.xyz);
	float dist = planeVector.w;

	// Use World Space Lerps
	pnorm = lerp(mul(unity_ObjectToWorld, pnorm), pnorm, useWorldSpace);
	dist = lerp(dist * length(pnorm), dist, useWorldSpace);
	wp -= lerp(mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz, float3(0, 0, 0), useWorldSpace);

	return dist - dot(wp, normalize(pnorm));
}

void applyClipPlane(float4 planeVector, float3 worldPos, float useWorldSpace) {
	clip(isClipped(planeVector, worldPos, useWorldSpace));
}
#endif