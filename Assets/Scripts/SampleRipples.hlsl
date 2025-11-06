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
        float2 offset = worldPos.xz - ripplePos.xz;
        float dist = length(offset);
        float angle = atan2(offset.y, offset.x);
        
       // --- Noise layering for complex edge breakup ---
        float2 dir = float2(cos(angle), sin(angle));
        float2 coord = dir * dist * 3.0;

        // Two noise layers: large smooth + fine detail
        float n1 = warpyNoise(coord * 1.5 + time * 0.1, time);
        float n2 = warpyNoise(coord * 8.0 - time * 0.3, time);
        float n = n1 * 0.6 + n2 * 0.4;

        // Modulate radius with noise
        float perturbedRadius = radius + (n - 0.5) * .32;

        // --- Edge shaping ---
        float fade = 1.0 - smoothstep(perturbedRadius, perturbedRadius + fadeWidth * 0.5, dist);
        fade = pow(fade, 1.6); // sharpen contrast

        // --- Glow falloff ---
        float glowDist = abs(dist - perturbedRadius);     // distance from ripple edge
        float glow = exp(-glowDist * 15.0);               // exponential falloff
        glow *= fade * 1.5;                               // tie to ripple strength
        
        // Add fadeout as ripple approaches maxDistance (fade out starting at 80% of maxDistance)
        float fadeoutStart = maxDistance * 0.8;
        float ageFade = 1.0 - smoothstep(fadeoutStart, maxDistance, radius); // when radius reaches fadeOutStart, start fading
                                                                             // until maxDistance is reached -> then ageFade = 0
        
        fade *= ageFade; // will be 0 when ageFade is 0 (i.e. ripple fully faded out)
        glow *= ageFade;
         
        // fade for most pixels will be really small, so only accumulate if significant
        if (fade > 0.001)
        {
            float3 glowColor = rippleColor * glow;        // brighter, emissive edge
            finalColor += rippleColor * fade + glowColor; // add both layers
            finalAlpha += fade + glow * 0.5;
        }
    }

    outColor = finalColor;
    outAlpha = saturate(finalAlpha);
}

#endif