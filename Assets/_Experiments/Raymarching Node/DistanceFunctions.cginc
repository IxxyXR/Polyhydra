static float vmax(float3 v) {
	return max(max(v.x, v.y), v.z);
}

// Sphere
// s: radius
float sdSphere(float3 p, float s)
{
	return length(p) - s;
}

// (Infinite) Plane
/// n.xyz: normal of the plane
/// n.w offset
float sdPlane(float3 p, float4 n)
{
	//n must be normalized
	return dot(p, n.xyz) + n.w;
}

// Box
// b: size of box in x/y/z
float sdBox(float3 p, float3 b)
{
	float3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) +
		length(max(d, 0.0));
}

// Cheap Box: distance to corners is overestimated
float fBoxCheap(float3 p, float3 b) { //cheap box
	return vmax(abs(p) - b);
}

//Rounded Box
float sdRoundBox(in float3 p, in float3 b, in float r)
{
	float3 q = abs(p) - b;
	return min(max(q.x, max(q.y, q.z)), 0.0) + length(max(q, 0.0)) - r;
}

// BOOLEAN OPERATORS //

// Union
float opU(float d1, float d2)
{
	return min(d1, d2);
}

// Subtraction
float opS(float d1, float d2)
{
	return max(-d1, d2);
}

// Intersection
float opI(float d1, float d2)
{
	return max(d1, d2);
}

// SMOOTH BOOLEAN OPERATORS

float opUS(float d1, float d2, float k) 
{
	float h = clamp(0.5 + 0.5*(d2 - d1) / k, 0.0, 1.0);
	return lerp(d2, d1, h) - k * h*(1.0 - h);
}

float opUS(float d1, float d2, float k, inout float h)
{
	h = clamp(0.5 + 0.5*(d2 - d1) / k, 0.0, 1.0);
	return lerp(d2, d1, h) - k * h*(1.0 - h);
}

float opSS(float d1, float d2, float k)
{
	float h = clamp(0.5 - 0.5*(d2 + d1) / k, 0.0, 1.0);
	return lerp(d2, -d1, h) + k * h*(1.0 - h);
}

float opIS(float d1, float d2, float k)
{
	float h = clamp(0.5 - 0.5*(d2 - d1) / k, 0.0, 1.0);
	return lerp(d2, d1, h) + k * h*(1.0 - h);
}

// Mod Position Axis
float pMod1(inout float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(-p + halfsize, size) - halfsize;
	return c;
}

// Mod Position Axis
float2 pMod2(inout float2 p, float2 size)
{
	float halfsize = size * 0.5;
	float c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(-p + halfsize, size) - halfsize;
	return c;
}

float3 pMod3(inout float3 p, float3 size)
{
	float halfsize = size * 0.5;
	float3 c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(-p + halfsize, size) - halfsize;
	return c;
}

float3 pMod3Lim(inout float3 p, float size, float3 l)
{
	float halfsize = size * 0.5;
	float3 c = floor((p + halfsize) / size);
	//p = fmod(p + halfsize, size) - halfsize;
	//p = fmod(-p + halfsize, size) - halfsize;
    p = p - halfsize * clamp(round(p / halfsize), -l, l);
	return c;
}