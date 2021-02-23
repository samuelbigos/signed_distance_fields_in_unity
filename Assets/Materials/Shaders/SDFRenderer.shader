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

            
            float4 _MainTex_ST;

            uniform sampler3D _sdfTexture;
            uniform float _sdfDistMod;
            uniform float _sdfRadius;
            uniform float _planetRadius;

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

            float sdSphere(float3 pos, float3 spherePos, float sphereRadius)
            {
                return length(pos - spherePos) - sphereRadius;
            }

            float sdf(float3 uv)
            {
                uv /= (_sdfRadius * 2.0);
                uv += float3(0.5, 0.5, 0.5);
                float sdf = tex3D(_sdfTexture, uv);
                sdf = (sdf * 2.0) - 1.0;
                sdf *= (_sdfRadius * 2.0);
                sdf *= _sdfDistMod;
                return sdf;
            }

            float3 sdfCol(float3 uv)
            {
                float2 colUV = float2(0.5, (length(uv) / _planetRadius));
                return tex2D(_planetTex, colUV);
            }

            bool outOfBounds(float3 pos)
            {
                if (length(pos) < _sdfRadius)
                {
                    return false;
                }
                return true;
            }

            float worldSample(float3 pos, out float3 col)
            {
                float dist = sdf(pos);
                col = sdfCol(pos);
                for (int i = 0; i < _boidCount; i++)
                {
                    dist = min(dist, sdSphere(pos, _boidPositions[i], _boidRadii[i]));
                }
                return dist;
            }

            float3 calcNormal(float3 p)
            {
                float h = 0.001;
                float2 k = float2(1, -1);
                float3 col;
                return normalize(k.xyy * worldSample(p + k.xyy * h, col) +
                    k.yyx * worldSample(p + k.yyx * h, col) +
                    k.yxy * worldSample(p + k.yxy * h, col) +
                    k.xxx * worldSample(p + k.xxx * h, col));
            }

            float rayMarch(float3 origin, float3 dir, float k, out float res, out float3 hitPos, out float3 colAtHit)
            {
                res = 1.0;
                float t = 0.0001;
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
                    t += dist;
                }
                return 0.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 rayOrigin = i.worldVertex;
                float3 rayDirection = normalize(i.worldVertex - _WorldSpaceCameraPos);

                // hit
                float res = 0.0;
                float3 hitPos = float3(0.0, 0.0, 0.0);
                float3 colAtHit = float3(1.0, 1.0, 1.0);
                float hit = rayMarch(rayOrigin, rayDirection, 16.0, res, hitPos, colAtHit);

                float s = 0.0;
                float ao = 0.0;
                float ambient = 0.0;
                if (hit > 0.0)
                {
                    float3 normal = calcNormal(hitPos);
                    float3 sunPos = float3(0.0, 10.0, 0.0);

                    // shadow                    
                    float3 sOrigin = hitPos + normal * 0.0005;
                    float3 sDir = normalize(sunPos - sOrigin);
                    float3 h;
                    float3 c;
                    ambient = dot(normal, sDir) * 0.3 + 0.7;
                    rayMarch(sOrigin, sDir, 4.0, res, h, c);
                    s = res;
                    s = ambient * lerp(0.5, 1.0, s);

                    // ao
                    float ao_dist = 0.025;
                    for (int x = -1; x < 2; x++)
                    {
                        for (int y = -1; y < 2; y++)
                        {
                            for (int z = -1; z < 2; z++)
                            {
                                float3 offset = normalize(float3(x, y, z)) * 0.025f;
                                float3 c;
                                ao += clamp(worldSample(hitPos + normal * ao_dist + offset, c) / ao_dist, 0.0, 1.0);
                            }
                        }
                    }
                    ao /= 27.0;
                    ao = 1.0 - pow(1.0 - ao, 3.0);
                }
                
                float3 col = colAtHit;//float3(255.0/255.0, 140.0/255.0, 0.0/255.0);
                col *= s;
                col *= ao;
                return float4(col, hit);
            }
            ENDCG
        }
    }
}