#include "DistanceFunctions.cginc"

float4 lambert (float3 normal, float3 light_direction)
{
	float ndotl = max(dot(normal, light_direction), 0);
	float4 c = float4(ndotl, ndotl, ndotl, 1);
	return saturate(c + 0.05);
}

float distance_function(float3 position, float3 center, float3 repeat, float radius, float3 limit)
{
    //float cell = pMod3(position, repeat);
    //return sdSphere(center + position, radius);
    position -= repeat * clamp(round(position/repeat), -limit, limit);
    return sdBox(center + position, float3(radius, radius/3.0, radius/3.0));
}

void raymarch_float (float3 Position, float3 Direction, float3 Center, float Radius, float3 LightDirection, float Steps, float MinDistance, float3 Repeat, float4 BackgroundColor, float3 Limit, out float4 Out, out float3 RayPosition)
{
    Out = BackgroundColor;
    RayPosition = Position;
    for(int i = 0; i < Steps; i++)
    {
        float distance = distance_function(Position, Center, Repeat, Radius, Limit);
        if (distance < MinDistance)
        {
        	const float eps = 0.01;
            float3 normal = normalize
            (	float3
                (	distance_function(Position + float3(eps, 0, 0), Center, Repeat, Radius, Limit) - distance_function(Position - float3(eps, 0, 0), Center, Repeat, Radius, Limit),
                    distance_function(Position + float3(0, eps, 0), Center, Repeat, Radius, Limit) - distance_function(Position - float3(0, eps, 0), Center, Repeat, Radius, Limit),
                    distance_function(Position + float3(0, 0, eps), Center, Repeat, Radius, Limit) - distance_function(Position - float3(0, 0, eps), Center, Repeat, Radius, Limit)
                )
            );
        	Out = lambert(normal, LightDirection);
            RayPosition = Position;
            break;
        }
        Position -= distance * Direction;
    }
}



