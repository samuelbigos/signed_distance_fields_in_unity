#ifndef SDF_INCLUDED
#define SDF_INCLUDED

static const float PI = 3.141592653589793238462;
static const float HIT_EPSILON = 0.001;

Texture3D<float> _sdfTexIn;
SamplerState sampler_sdfTexIn;
uniform uint _sdfTexSizeX;
uniform uint _sdfTexSizeY;
uniform uint _sdfTexSizeZ;
uniform float _sdfMaxDist;
uniform float _sdfRadius;

uniform int _aoSamples;
uniform float _aoKernelSize;
uniform float _aoThreshold;
uniform float _aoContribution;

uniform int _shadowSamples;
uniform float _shadowKernelSize;
uniform float _shadowRayOffset;
uniform float _shadowK;
uniform float _shadowContribution;

// SDF sampling functions
float pack(float dist)
{
    dist /= _sdfMaxDist;
    dist = dist * 0.5 + 0.5;
    return clamp(dist, 0.0, 1.0);
}
float unpack(float dist)
{
    dist = dist * 2.0 - 1.0;
    return dist * _sdfMaxDist;
}
float3 kernelToUV(uint3 k)
{
    float3 uv = float3(float(k.x), float(k.y), float(k.z)) + float3(0.5f, 0.5f, 0.5f);
    return uv / float3(float(_sdfTexSizeX), float(_sdfTexSizeY), float(_sdfTexSizeZ));
}
float3 uvToWorld(float3 uv)
{
    uv -= float3(0.5, 0.5, 0.5);
    uv *= _sdfRadius * 2.0;
    return uv;
}
float3 worldToUV(float3 world)
{
    world /= _sdfRadius * 2.0;
    world += float3(0.5, 0.5, 0.5);
    return world;
}
float sdfUV(float3 uv)
{
    return unpack(_sdfTexIn.SampleLevel(sampler_sdfTexIn, uv, 0).x);
}
float sdfWorld(float3 pos)
{
    return sdfUV(worldToUV(pos));
}
bool rayHit(float3 pos, out float dist)
{
    dist = sdfWorld(pos);
    return dist <= HIT_EPSILON;
}

// Helpers
bool outOfBounds(float3 pos)
{
    return length(pos) > _sdfRadius;
}
float3 calcNormal(float3 p)
{
    float h = 0.001;
    float2 k = float2(1, -1);
    return normalize(k.xyy * sdfWorld(p + k.xyy * h) +
        k.yyx * sdfWorld(p + k.yyx * h) +
        k.yxy * sdfWorld(p + k.yxy * h) +
        k.xxx * sdfWorld(p + k.xxx * h));
}
float remap(float from, float to, float value)
{
    return (value - from) / (to - from);
}

// Eikonal correction
float calcEikonalCorrection(float3 dir)
{
    float3 nDir = normalize(dir);
    float eikonalFix = min(nDir.x, min(nDir.y, nDir.z));
    float l = 0.70711f;
    eikonalFix = remap(0.70711, 1.0, eikonalFix);
    eikonalFix = lerp(0.99, 1.0, eikonalFix);
    return eikonalFix;
}

// SDF lighting
float shadowRay(float3 origin, float3 dir, float k)
{
    float dist;
    dir = normalize(dir);
    float res = 1.0;
    float t = _shadowRayOffset;
    float3 ray = origin + dir * t;
    [loop]
    for (int i = 0; i < 128; i++)
    {
        if (rayHit(ray, dist))
        {
            return 0.0;
        }
        if (outOfBounds(ray))
        {
            break;
        }
        float kDist = dist * k;
        res = min(res, kDist / min(t, 1.0));
        t += max(0.01, dist);
        ray = origin + dir * t;
    }
    return res;
}
float shadowCalc(float3 pos, float3 normal, float3 sunPos)
{
    float3 sunDir = normalize(sunPos - pos);
    float3 rayOrigin = pos + normal * _shadowRayOffset;

    float3 oy = normalize(pos - ddy(pos)) * _shadowKernelSize;
    float3 ox = normalize(pos - ddx(pos)) * _shadowKernelSize;
    float s = 0.0;

    for (int x = 0; x < _shadowSamples; x++)
    {
        for (int y = 0; y < _shadowSamples; y++)
        {
            float fx = float(x) - (float(_shadowSamples - 1) / 2.0);
            float fy = float(y) - (float(_shadowSamples - 1) / 2.0);
            float3 origin = rayOrigin + oy * fy + ox * fx;
            s += shadowRay(origin, sunDir, _shadowK);
        }
    }
    s /= float(_shadowSamples * _shadowSamples);
    return s * _shadowContribution + (1.0 - _shadowContribution);
}
// SDF Ambient Occlusion
float aoCalc(float3 pos, float3 normal)
{
    // fibonacci sphere
    // https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
    float phi = PI * (3.0 - sqrt(5.0));
    float sum = 0.0;
    [loop]
    for (int i = 0; i < _aoSamples; i++)
    {
        float3 p = float3(0.0, 0.0, 0.0);
        p.y = 1.0 - (i / float(_aoSamples - 1.0)) * 2.0;
        float radius = sqrt(1.0 - pow(p.y, 2.0));
        float theta = phi * float(i);
        p.x = cos(theta) * radius;
        p.z = sin(theta) * radius;

        sum += saturate(sdfWorld(pos + p * _aoKernelSize) / _aoKernelSize);
    }
    sum /= float(_aoSamples);
    sum = 1.0 - pow(1.0 - sum, 2.0);
    sum = min(1.0, sum * _aoThreshold);
    return lerp(1.0, sum, _aoContribution);
}

// Raymarch
float rayMarch(float3 origin, float3 dir, float k, out float res, out float3 hitPos)
{
    // Running eikonal equation on the SDF causes distance values to elongate along non-cardinal directions,
    // the more diagonal the more pronounced. Here, adjust the distance value by the amount of cardinality.
    float eikonalFix = calcEikonalCorrection(dir);
    
    res = 1.0;
    float t = 0.001;
    [loop]
    for (int i = 0; i < 256; i++)
    {
        float3 current = origin + dir * t;
        if (outOfBounds(current))
        {
            return 0.0;
        }
        float dist = sdfWorld(current);
        if (dist <= 0.0)
        {
            hitPos = current;
            res = 0.0;
            return 1.0;
        }
        dist = max(dist, 0.0001);
        res = min(res, k * dist / t);
        t += dist * eikonalFix;
    }
    return 0.0;
}

// SDF operations
float sdSphere(float3 pos, float3 spherePos, float sphereRadius)
{
    return length(pos - spherePos) - sphereRadius;
}
float opSubtraction(float d1, float d2)
{
    return max(-d1,d2);
}
float opSmoothSubtraction(float d1, float d2, float k) 
{
    float h = max(k-abs(-d1-d2),0.0);
    return max(-d1, d2) + h*h*0.25/k;
}
float opUnion(float d1, float d2)
{
    return min(d1,d2);
}
float opSmoothUnion(float d1, float d2, float k) 
{
    float h = max(k-abs(d1-d2),0.0);
    return min(d1, d2) - h*h*0.25/k;
}

#endif // SDF_INCLUDED