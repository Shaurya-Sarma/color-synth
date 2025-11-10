#ifndef SAMPLE_RIPPLES_INCLUDED
#define SAMPLE_RIPPLES_INCLUDED

float2 fade(float2 t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

float grad2(float2 p, float2 ip, float2 fp)
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

    float a = grad2(i + float2(0.0, 0.0), i, f);
    float b = grad2(i + float2(1.0, 0.0), i, f);
    float c = grad2(i + float2(0.0, 1.0), i, f);
    float d = grad2(i + float2(1.0, 1.0), i, f);

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}



// Fractal/layered noise for more organic look
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

// Sample multiple ripples from textures
void SampleRipples_float(
    float MaxRipples,
    float3 worldPos,
    float time,
    UnityTexture2D RippleDataTex,
    UnityTexture2D RippleTimeTex,
    UnityTexture2D RippleColorTex,
    float noiseStrength,
    out float3 outColor,
    out float outAlpha
)
{
    float3 finalColor = float3(0,0,0);
    float finalAlpha = 0;

    int rippleCount = (int)MaxRipples;

    for (int i = 0; i < rippleCount; i++)
    {
        // compute UV to sample ripple data from textures
        float2 uv = float2((i + 0.5) / rippleCount, 0.5); // gets middle of texel
        
        float4 dataSample = SAMPLE_TEXTURE2D(RippleDataTex, sampler_RippleDataTex, uv);
        float3 ripplePos = dataSample.xyz;
        float speed = dataSample.w;

        float4 timeSample = SAMPLE_TEXTURE2D(RippleTimeTex, sampler_RippleTimeTex, uv);
        float startTime = timeSample.r;
        float maxDistance = timeSample.g;
        float fadeWidth = timeSample.b;
        float noiseScale = timeSample.a; 
        
        float4 colorSample = SAMPLE_TEXTURE2D(RippleColorTex, sampler_RippleColorTex, uv);
        float3 rippleColor = colorSample.rgb;

        float elapsed = time - startTime;

        // Skip if this is an empty/cleared ripple slot
        if (elapsed < 0 || maxDistance <= 0) continue;
        
        // Calculate current radius of the ripple -> expands over time 
        float radius = elapsed * speed;
        radius = min(radius, maxDistance); // clamp to maxDistance

        // Calculate distance and angle from ripple center
        float3 offset3D = worldPos - ripplePos;
        float dist = length(offset3D);

      // XZ plane for horizontal noise
        float2 offsetXZ = offset3D.xz;
        float distXZ = length(offsetXZ); // horizontal distance for noise
        float angle = atan2(offsetXZ.y, offsetXZ.x);
        float2 dir = float2(cos(angle), sin(angle));
        float2 coord = dir * distXZ * noiseScale; // only horizontal

        // Two noise layers: large smooth + fine detail
        float n1 = warpyNoise(coord * 1.5 + time * 0.1, time);
        float n2 = warpyNoise(coord * 8.0 - time * 0.3, time);
        float n = n1 * 0.6 + n2 * 0.4; // combine layers

        // Modulate radius with noise
        float perturbedRadius = radius + (n - 0.5) * noiseStrength;

        // calculate fade based on distance from ripple center
        float fade = 1.0 - smoothstep(perturbedRadius, perturbedRadius + fadeWidth * 0.5, dist);

        // add glow at ripple edge
        float glowStrength = 16.0;
        float glowDist = abs(dist - perturbedRadius); // distance from ripple edge
        float glow = exp(-glowDist * glowStrength); // 1 means brightest at edge, falls off quickly
        glow *= fade * 1.5; // make glow exist where ripple is visible                               

        // fade out ripple as it ages
        // could be interesting to explore after-image effects
        // could do based on % reach of max distance, earlier code commits have examples
        float fadeDuration = (maxDistance / speed) * 0.3; // fade lasts 30% of ripple lifetime
        float ageFade = saturate(1.0 - (elapsed - (maxDistance / speed) * 0.8) / fadeDuration);
            
        fade *= ageFade; // will be 0 when ageFade is 0 (i.e. ripple fully faded out)
        glow *= ageFade; // also fade glow with age
                         //* interesting effect when we remove this line -> ripple edge line persists
                         //* when the ripple fades out

        // fade for most pixels will be really small, so only accumulate if significant
        if (fade > 0.001)
        {
            float3 glowColor = rippleColor * glow;        
            finalColor += rippleColor * fade + glowColor; // add both layers
            finalAlpha += fade + glow * 0.5;
        }
    }

    outColor = finalColor;
    outAlpha = saturate(finalAlpha);
}

#endif