#ifndef SAMPLE_RIPPLES_INCLUDED
#define SAMPLE_RIPPLES_INCLUDED

// Simple hash function for noise
float hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// 2D value noise for organic boundaries
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    
    // Smooth interpolation
    f = f * f * (3.0 - 2.0 * f);
    
    // Four corners of the cell
    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));
    
    // Bilinear interpolation
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Fractal/layered noise for more organic look
float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    
    for (int i = 0; i < 3; i++)
    {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    
    return value;
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
        
        // Create noise coordinate based on angle for circular variation
        float2 noiseCoord = float2(angle * 3.0, dist * 0.5) * noiseScale;
        
        // Add ripple-specific seed to make each ripple unique
        noiseCoord += float2(i * 123.456, i * 789.012);
        
        // Get organic noise value (0 to 1 range, centered at 0.5)
        float noiseValue = fbm(noiseCoord) - 0.5;
        
        // Perturb the radius with noise for irregular boundary
        float smoothedNoise = lerp(0, noiseValue, 0.7); // dampens extremes
        float perturbedRadius = radius + smoothedNoise * noiseStrength;

        // Calculate fade based on distance from perturbed radius
        float fade = 1.0 - smoothstep(perturbedRadius, perturbedRadius + fadeWidth, dist);
        
        // Add fadeout as ripple approaches maxDistance (fade out starting at 80% of maxDistance)
        float fadeoutStart = maxDistance * 0.8;
        float ageFade = 1.0 - smoothstep(fadeoutStart, maxDistance, radius); // when radius reaches fadeOutStart, start fading
                                                                             // until maxDistance is reached -> then ageFade = 0
        
        fade *= ageFade; // will be 0 when ageFade is 0 (i.e. ripple fully faded out)
         
        // fade for most pixels will be really small, so only accumulate if significant
        if (fade > 0.001)
        {
            finalColor += rippleColor * fade;
            finalAlpha += fade;
        }
    }

    outColor = finalColor;
    outAlpha = saturate(finalAlpha);
}

#endif