Shader "Unlit/SDFRenderer"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        //Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Includes/SDF.cginc"
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldVertex : TEXCOORD0;
                float3 vectorToSurface : TEXCOORD1;
            };

            uniform float _boidCount;
            uniform float3 _boidPositions[256];
            uniform float _boidRadii[256];

            uniform float _sphereRadius;
            uniform float _planetTexHeight;
            uniform sampler2D _planetTex;
            uniform int _debugMode = 0;
            uniform float3 _sunPos;
            uniform float _shadowIntensity;
            uniform float _shadowSoftness;
            
            v2f vert(appdata v)
            {
                v2f o;

                o.worldVertex = mul(unity_ObjectToWorld, v.vertex);
                o.vectorToSurface = o.worldVertex - _WorldSpaceCameraPos;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float3 sdfCol(float3 uv)
            {
                float2 colUV = float2(0.5, (length(uv) / _sphereRadius));
                return tex2D(_planetTex, colUV);
            }

            float worldSample(float3 pos, out float3 col)
            {
                float dist = sdfWorld(pos);
                col = float3(0.8, 0.8, 0.8);
                if (dist < hitThreshold())
                {
                    col = sdfCol(pos);
                }                
                for (int i = 0; i < _boidCount; i++)
                {
                    dist = min(dist, sdSphere(pos, _boidPositions[i], _boidRadii[i]));
                }
                return dist;
            }

            float3 worldNormal(float3 p)
            {
                float h = 0.001;
                float2 k = float2(1, -1);
                float3 col;
                return normalize(k.xyy * worldSample(p + k.xyy * h, col) +
                    k.yyx * worldSample(p + k.yyx * h, col) +
                    k.yxy * worldSample(p + k.yxy * h, col) +
                    k.xxx * worldSample(p + k.xxx * h, col));
            }

            float worldRayMarch(float3 origin, float3 dir, float k, out float res, out float3 hitPos, out float3 colAtHit)
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
                    float dist = worldSample(current, colAtHit);
                    if (dist <= hitThreshold())
                    {
                        hitPos = current;
                        res = 0.0;
                        return 1.0;
                    }
                    dist = max(dist, 0.0001) * eikonalFix;
                    res = min(res, k * dist / t);
                    t += dist;
                }
                return 1.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 rayOrigin = i.worldVertex;
                float3 rayDirection = normalize(i.worldVertex - _WorldSpaceCameraPos);

                if (_debugMode == 1) // 2d sdf cross section
                {
                    float3 c;
                    float d = worldSample(rayOrigin, c);

                    // coloring
                    float3 col = float3(step(0.0, d), step(d, 0.0), 0.0);
                    col *= 1.0 - exp(-5.0 * abs(d));
                    col *= 0.8 + 0.2 * cos(512.0 * abs(d));
                    col = lerp(col, float3(1.0, 1.0, 1.0), 1.0 - smoothstep(0.0, 0.005, abs(d)));

                    return float4(col, 1.0);
                }
                
                // project forward to the start of the SDF
                float dist = raySphereIntersect(rayOrigin, rayDirection, float3(0.0, 0.0, 0.0), _sdfRadius);
                rayOrigin += rayDirection * (dist + 0.01);
                
                // hit
                float res = 0.0;
                float3 hitPos = float3(0.0, 0.0, 0.0);
                float3 colAtHit = float3(1.0, 1.0, 1.0);
                float hit = worldRayMarch(rayOrigin, rayDirection, 16.0, res, hitPos, colAtHit);

                float s = 0.0;
                float ao = 0.0;
                float ambient = 0.0;
                if (hit > 0.0)
                {
                    float3 normal = worldNormal(hitPos);

                    // Shadow                    
                    float3 sOrigin = hitPos + normal * 0.001;
                    float3 sDir = normalize(_sunPos - hitPos);
                    float3 h, c;

                    ambient = dot(normal, sDir) * 0.25 + 1.0;
                    worldRayMarch(sOrigin, sDir, lerp(0.0, 32.0, 1.0 - _shadowSoftness), res, h, c);
                    s = ambient * lerp(1.0 - _shadowIntensity, 1.0, res);
                    
                    // AO
                    float phi = PI * (3.0 - sqrt(5.0));
                    [loop]
                    for (int i = 0; i < _aoSamples; i++)
                    {
                        float3 p = float3(0.0, 0.0, 0.0);
                        p.y = 1.0 - i / float(_aoSamples) * 2.0;
                        float radius = sqrt(1.0 - pow(p.y, 2.0));
                        float theta = phi * float(i);
                        p.x = cos(theta) * radius;
                        p.z = sin(theta) * radius;
                        ao += saturate(worldSample(hitPos + p * _aoKernelSize, c) / _aoKernelSize);
                    }
                    ao /= float(_aoSamples) * 0.5;
                    ao = 1.0 - pow(1.0 - ao, 2.0);

                    float3 col = colAtHit;
                    col *= s;
                    col *= ao;
                    
                    return float4(col, hit);
                }
                return float4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
    }
}