using System.Collections.Generic;
using Boids;
using UImGui;
using UnityEngine;

namespace SDF
{
    public class SDFRenderer : MonoBehaviour
    {
        [SerializeField] private Texture2D PlanetTexture;
        
        public Material SDFRendererMat => _sdfRendererMat;
        
        private Material _sdfRendererMat;
        private Transform _trans;

        private static readonly int BOID_COUNT = Shader.PropertyToID("_boidCount");
        private static readonly int BOID_POSITIONS = Shader.PropertyToID("_boidPositions");
        private static readonly int BOID_RADII = Shader.PropertyToID("_boidRadii");
        private static readonly int PLANET_TEX = Shader.PropertyToID("_planetTex");
        private static readonly int PLANET_TEX_HEIGHT = Shader.PropertyToID("_planetTexHeight");
        private static readonly int DEBUG_MODE = Shader.PropertyToID("_debugMode");
        private static readonly int AO_SAMPLES = Shader.PropertyToID("_aoSamples");
        private static readonly int AO_KERNEL_SIZE = Shader.PropertyToID("_aoKernelSize");
        private static readonly int MAX_RAYMARCH_STEPS = Shader.PropertyToID("_maxRaymarchSteps");
        private static readonly int SDF_TEX_IN = Shader.PropertyToID("_sdfTexIn");

        private void Awake()
        {
            _trans = transform;
        }

        private void Start()
        {
            _sdfRendererMat = GetComponent<MeshRenderer>().material;
            _sdfRendererMat.SetVectorArray(BOID_POSITIONS, new Vector4[256]);
            _sdfRendererMat.SetFloatArray(BOID_RADII, new float[256]);
            _sdfRendererMat.SetTexture(PLANET_TEX, PlanetTexture);
            _sdfRendererMat.SetFloat(PLANET_TEX_HEIGHT, PlanetTexture.height);
        }

        private void Update()
        {
            Shader.SetGlobalInt(DEBUG_MODE, DebugWindow.Instance.DebugDrawMode);
            if (DebugWindow.Instance.DebugDrawMode == 1)
            {
                Vector3 p = _trans.localPosition;
                p.z = 1.0f;
                _trans.localPosition = p;
            }
            else
            {
                Vector3 p = _trans.localPosition;
                p.z = 0.11f;
                _trans.localPosition = p;
            }
            
            _sdfRendererMat.SetInt(AO_SAMPLES, DebugWindow.Instance.AOSamples);
            _sdfRendererMat.SetFloat(AO_KERNEL_SIZE, DebugWindow.Instance.AOKernelSize);
            _sdfRendererMat.SetInt(MAX_RAYMARCH_STEPS, DebugWindow.Instance.MaxRaymarchSteps);
        }

        public void SetSDFTexture(RenderTexture tex)
        {
            _sdfRendererMat.SetTexture(SDF_TEX_IN, tex);
        }

        public void SetBoids(List<BoidController.Boid> boids)
        {
            _sdfRendererMat.SetFloat(BOID_COUNT, boids.Count);
            if (boids.Count > 0)
            {
                Vector4[] posArray = new Vector4[boids.Count];
                float[] radArray = new float[boids.Count];
                for (int i = 0; i <boids.Count; i++)
                {
                    posArray[i] = boids[i].Position;
                    radArray[i] = boids[i].Radius;
                }
                _sdfRendererMat.SetVectorArray(BOID_POSITIONS, posArray);
                _sdfRendererMat.SetFloatArray(BOID_RADII, radArray);
            }
        }
    }
}
