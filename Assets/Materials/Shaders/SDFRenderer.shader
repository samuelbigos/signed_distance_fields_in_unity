Shader "Unlit/SDFRenderer"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../../Includes/SDF.cginc"
            
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

            uniform float _sphereRadius;

            uniform float _boidCount;
            uniform float3 _boidPositions[256];
            uniform float _boidRadii[256];

            uniform float _planetTexHeight;
            uniform sampler2D _planetTex;

            v2f vert(appdata v)
            {
                v2f o;

                o.worldVertex = mul(unity_ObjectToWorld, v.vertex);
                o.vectorToSurface = o.worldVertex - _WorldSpaceCameraPos;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float3 sdfCol(float3 pos)
            {
                float2 colUV = float2(0.5, length(pos) / _sphereRadius);
                return tex2D(_planetTex, colUV);
            }

            float worldSample(float3 pos, out float3 col)
            {
                float dist = sdfWorld(pos);
                col = float3(1.0, 1.0, 1.0);
                if (dist < HIT_EPSILON)
                {
                    col = sdfCol(pos);
                }                
                for (int i = 0; i < _boidCount; i++)
                {
                    dist = min(dist, sdSphere(pos, _boidPositions[i], _boidRadii[i]));
                }
                return dist;
            }

            float rayMarchWorld(float3 origin, float3 dir, float k, out float res, out float3 hitPos, out float3 colAtHit)
            {
                // Running eikonal equation on the SDF causes distance values to elongate along non-cardinal directions,
                // the more diagonal the more pronounced. Here, adjust the distance value by the amount of cardinality.
                float eikonalFix = calcEikonalCorrection(dir);

                res = 1.0;
                float t = 0.0;
                [loop]
                for (int i = 0; i < 256; i++)
                {
                    float3 current = origin + dir * t;
                    if (outOfBounds(current))
                    {
                        return 0.0;
                    }
                    float dist = worldSample(current, colAtHit);
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

            fixed4 frag(v2f i) : SV_Target
            {
                float3 rayOrigin = i.worldVertex;
                float3 rayDirection = normalize(i.worldVertex - _WorldSpaceCameraPos);
                
                float res = 0.0;
                float3 hitPos = float3(0.0, 0.0, 0.0);
                float3 colAtHit = float3(1.0, 1.0, 1.0);
                float hit = rayMarchWorld(rayOrigin, rayDirection, 16.0, res, hitPos, colAtHit);

                float s = 0.0;
                float ao = 0.0;
                float ambient = 0.0;
                if (hit > 0.0)
                {
                    float3 normal = calcNormal(hitPos);
                    float3 sunPos = float3(0.0, 10.0, 0.0);

                    // Shadow                    
                    float3 sOrigin = hitPos + normal * 0.01;
                    float3 sDir = normalize(sunPos - hitPos);
                    float3 h, c;

                    ambient = dot(normal, sDir) * 0.3 + 0.7;
                    s = 1.0 - rayMarchWorld(sOrigin, sDir, 8.0, res, h, c);
                    s = ambient * lerp(0.5, 1.0, res);
                    
                    // AO
                    float phi = PI * (3.0 - sqrt(5.0));
                    [loop]
                    for (int i = 0; i < _aoSamples; i++)
                    {
                        float3 p = float3(0.0, 0.0, 0.0);
                        p.y = 1.0 - i / float(_aoSamples - 1.0) * 2.0;
                        float radius = sqrt(1.0 - pow(p.y, 2.0));
                        float theta = phi * float(i);
                        p.x = cos(theta) * radius;
                        p.z = sin(theta) * radius;
                        ao += saturate(worldSample(hitPos + p * _aoKernelSize, c) / _aoKernelSize);
                    }
                    ao /= float(_aoSamples) * 0.25;
                }
                
                float3 col = colAtHit;
                col *= s;
                col *= ao;
                return float4(col, hit);
            }
            ENDCG
        }
    }
}