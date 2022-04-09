#ifndef SDF_INCLUDED
#define SDF_INCLUDED

static const float PI = 3.141592653589793238462;

Texture3D<float> _sdfTexIn;
SamplerState sampler_sdfTexIn;
uniform uint _sdfTexSizeX;
uniform uint _sdfTexSizeY;
uniform uint _sdfTexSizeZ;
uniform float _sdfMaxDist;
uniform float _sdfRadius;

uniform int _aoSamples;
uniform float _aoKernelSize;

uniform int _shadowSamples;
uniform float _shadowKernelSize;
uniform float _shadowRayOffset;
uniform float _shadowK;
uniform float _shadowContribution;

uniform int _maxRaymarchSteps;

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
float hitThreshold()
{
    return 0.000001f;
}
bool rayHit(float3 pos, out float dist)
{
    dist = sdfWorld(pos);
    return dist <= hitThreshold();
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
float raySphereIntersect(float3 r0, float3 rd, float3 s0, float sr)
{
    float a = dot(rd, rd);
    float3 s0_r0 = r0 - s0;
    float b = 2.0 * dot(rd, s0_r0);
    float c = dot(s0_r0, s0_r0) - (sr * sr);
    if (b * b - 4.0 * a * c < 0.0)
    {
        return -1.0;
    }
    return (-b - sqrt(b * b - 4.0 * a * c)) / (2.0 * a);
}

// Eikonal correction
float calcEikonalCorrection(float3 dir)
{
    float3 nDir = normalize(dir);
    float eikonalFix = min(nDir.x, min(nDir.y, nDir.z));
    float l = 0.70711f;
    eikonalFix = remap(0.70711, 1.0, eikonalFix);
    eikonalFix = lerp(0.98, 1.0, eikonalFix);
    return eikonalFix;
}

// Raymarch
float rayMarch(float3 origin, float3 dir, float k, out float res, out float3 hitPos)
{
    // Running eikonal equation on the SDF causes distance values to elongate along non-cardinal directions,
    // the more diagonal the more pronounced. Here, adjust the distance value by the amount of cardinality.
    float eikonalFix = calcEikonalCorrection(dir);
    
    res = 1.0;
    float t = 0.0;
    [loop]
    for (int i = 0; i < _maxRaymarchSteps; i++)
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
        dist = max(dist, 0.0);
        res = min(res, k * dist / t);
        t += dist * eikonalFix;
    }
    return 1.0;
}

// SDF operations
float sdBox(float3 p, float3 b)
{
    float3 q = abs(p) - b;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}
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