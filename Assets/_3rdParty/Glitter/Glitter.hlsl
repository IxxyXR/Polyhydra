
void Glitter_float(
    float2 uv0,
    float2 uv1,
    float3 tangentDir,
    float3 bitangentDir,
    float3 normalDir,
    float3 posWorld,
    float4 vertexColor,
    Texture2D GlitterNormals,
    float GlitterEdgesFalloff,
    float4 GlitterColor,
    float GlitterFrequency,
    Texture2D GlitterMask,
    float GlitterDensityFactor,
    Texture2D MainTex,
    Texture2D MainHeightMap,
    float Depth,
    float BaseDepth,
    Texture2D GlitterHeightMap,
    float GlitterDepth,
    float GlitterBaseDepth,
    Texture2D TintTexture,
    SamplerState sampleState,

    out float3 Out
    ) {
        bool facing = 0;
        float isFrontFace = ( facing >= 0 ? 1 : 0 );
        float faceSign = ( facing >= 0 ? 1 : -1 );

        normalDir = normalize(normalDir);
        normalDir *= faceSign;

        float3x3 tangentTransform = float3x3(tangentDir, bitangentDir, normalDir);
        float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - posWorld.xyz);
        float3 normalDirection = normalDir;



        float4 MainHeightMap_var = MainHeightMap.Sample(sampleState, uv0);
        float4 MainTex_var = MainTex.Sample(sampleState, (float3(uv0,0.0)+(mul(tangentTransform, viewDirection ).xyz.rgb*(MainHeightMap_var.r-BaseDepth)*Depth)).rg);
        float3 MainColor = (MainTex_var.rgb*vertexColor.rgb * TintTexture.Sample(sampleState, uv1));



        float4 GlitterHeightMap_var = GlitterHeightMap.Sample(sampleState, uv0);
        float2 GlitterParallaxUVOffset = (float3(uv0,0.0)+(mul( tangentTransform, viewDirection ).xyz.rgb*(GlitterHeightMap_var.r-GlitterBaseDepth)*GlitterDepth)).rg;
        float3 GlitterNormals_var = GlitterNormals.Sample(sampleState, GlitterParallaxUVOffset);
        float4 GlitterMask_var = GlitterMask.Sample(sampleState, GlitterParallaxUVOffset);
        float3 Glitter = (GlitterColor.rgb*(saturate(
            (
                GlitterDensityFactor +
                ((sin((abs(dot(
                    mul(GlitterNormals_var.rgb, tangentTransform).xyz.rgb,
                    viewDirection))*GlitterFrequency)) + 1.0)
                 * (1.0 - GlitterDensityFactor)
             ) / 2.0))
            * GlitterMask_var.r*pow(abs(dot(normalDir,viewDirection)),GlitterEdgesFalloff))
            * GlitterColor.a
        );

        float3 emissive = (MainColor+Glitter);
        Out = emissive;

}
