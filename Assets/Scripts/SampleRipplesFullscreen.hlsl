#ifndef SAMPLE_RIPPLES_FULLSCREEN_INCLUDED
#define SAMPLE_RIPPLES_FULLSCREEN_INCLUDED

// ------------------- Noise helpers -------------------
float2 fade(float2 t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

float grad2(float2 p)
{
    float2 u = float2(12.9898, 78.233);
    float a = dot(p, u);
    return frac(sin(a) * 43758.5453);
}

float perlinNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = fade(f);

    float a = grad2(i + float2(0.0, 0.0));
    float b = grad2(i + float2(1.0, 0.0));
    float c = grad2(i + float2(0.0, 1.0));
    float d = grad2(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 3; i++)
    {
        value += amplitude * perlinNoise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

float warpyNoise(float2 p, float time)
{
    float2 q = float2(
        fbm(p + float2(0.0, 0.0) + time * 0.05),
        fbm(p + float2(5.2, 1.3) + time * 0.05)
    );
    
    float2 r = float2(
        fbm(p + 1.2*q + float2(1.7, 9.2)),
        fbm(p + 1.2*q + float2(8.3, 2.8))
    );
    
    return fbm(p + 0.8*r);
}


// ------------------- Main ripple function -------------------
void SampleRipplesFullscreen_float(
    float MaxRipples,
    float3 worldPos,
    float2 screenUV,
    float time,
    UnityTexture2D RippleDataTex, 
    UnityTexture2D RippleTimeTex,
    UnityTexture2D RippleColorTex, 
    float noiseStrength,
    out float3 outColor,
    out float outAlpha
)
{

    // --- Ripple accumulation ---
    float3 finalColor = float3(0,0,0);
    float finalAlpha = 0;

    int rippleCount = (int)MaxRipples;

    for (int i = 0; i < rippleCount; i++)
    {
        float2 uvRipple = float2((i + 0.5) / rippleCount, 0.5);

        float4 dataSample = RippleDataTex.Sample(sampler_RippleDataTex, uvRipple);
        float3 ripplePos = dataSample.xyz;
        float speed = dataSample.w;

        float4 timeSample = RippleTimeTex.Sample(sampler_RippleTimeTex, uvRipple);
        float startTime = timeSample.r;
        float maxDistance = timeSample.g;
        float fadeWidth = timeSample.b;
        float noiseScale = timeSample.a;

        float4 colorSample = RippleColorTex.Sample(sampler_RippleColorTex, uvRipple);
        float3 rippleColor = colorSample.rgb;

        float elapsed = time - startTime;
        if (elapsed < 0 || maxDistance <= 0) continue;

        float radius = min(elapsed * speed, maxDistance);

        float3 offset3D = worldPos - ripplePos;
        float dist = length(offset3D);

        float2 offsetXZ = offset3D.xz;
        float distXZ = length(offsetXZ);
        float angle = atan2(offsetXZ.y, offsetXZ.x);
        float2 dir = float2(cos(angle), sin(angle));
        float2 coord = dir * distXZ * noiseScale;

        float n1 = warpyNoise(coord * 1.5 + time * 0.1, time);
        float n2 = warpyNoise(coord * 8.0 - time * 0.3, time);
        float n = n1 * 0.6 + n2 * 0.4;

        float perturbedRadius = radius + (n - 0.5) * noiseStrength;

        float fade = 1.0 - smoothstep(perturbedRadius, perturbedRadius + fadeWidth * 0.5, dist);

        float glowStrength = 16.0;
        float glowDist = abs(dist - perturbedRadius);
        float glow = exp(-glowDist * glowStrength) * fade * 1.5;

        float fadeDuration = (maxDistance / speed) * 0.3;
        float ageFade = saturate(1.0 - (elapsed - (maxDistance / speed) * 0.8) / fadeDuration);

        fade *= ageFade;
        glow *= ageFade;

        if (fade > 0.001)
        {
            rippleColor += float3(0.02 * sin(time + dist), 0.01, -0.02 * cos(time * 0.5));
            float3 glowColor = rippleColor * glow;
            finalColor += rippleColor * fade + glowColor;
            finalAlpha += fade + glow * 0.5;
        }
    }

    outColor = finalColor;
    outAlpha = saturate(finalAlpha);
}

#endif
