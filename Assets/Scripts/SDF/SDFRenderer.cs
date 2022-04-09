using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Boids;
using UImGui;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SDF
{
    public class SDFRenderer : MonoBehaviour
    {
        [SerializeField] private Texture2D _planetTexture;
        
        public Material SDFRendererMat => _sdfRendererMat;
        
        private Material _sdfRendererMat;
        private Transform _trans;

        private static readonly int _boidCount = Shader.PropertyToID("_boidCount");
        private static readonly int _boidPositions = Shader.PropertyToID("_boidPositions");
        private static readonly int _boidRadii = Shader.PropertyToID("_boidRadii");

        private void Awake()
        {
            _trans = transform;
        }

        private void Start()
        {
            _sdfRendererMat = GetComponent<MeshRenderer>().material;
            _sdfRendererMat.SetVectorArray("_boidPositions", new Vector4[256]);
            _sdfRendererMat.SetFloatArray("_boidRadii", new float[256]);
            _sdfRendererMat.SetTexture("_planetTex", _planetTexture);
            _sdfRendererMat.SetFloat("_planetTexHeight", (float)_planetTexture.height);
        }

        private void Update()
        {
            Shader.SetGlobalInt("_debugMode", DebugWindow.Instance.DebugDrawMode);
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
            
            _sdfRendererMat.SetInt("_aoSamples", DebugWindow.Instance.AOSamples);
            _sdfRendererMat.SetFloat("_aoKernelSize", DebugWindow.Instance.AOKernelSize);
            _sdfRendererMat.SetInt("_maxRaymarchSteps", DebugWindow.Instance.MaxRaymarchSteps);
        }

        public void SetSDFTexture(RenderTexture tex)
        {
            _sdfRendererMat.SetTexture("_sdfTexIn", tex);
        }

        public void SetBoids(List<BoidController.Boid> boids)
        {
            _sdfRendererMat.SetFloat(_boidCount, boids.Count);
            if (boids.Count > 0)
            {
                Vector4[] posArray = new Vector4[boids.Count];
                float[] radArray = new float[boids.Count];
                for (int i = 0; i <boids.Count; i++)
                {
                    posArray[i] = boids[i].Position;
                    radArray[i] = boids[i].Radius;
                }
                _sdfRendererMat.SetVectorArray(_boidPositions, posArray);
                _sdfRendererMat.SetFloatArray(_boidRadii, radArray);
            }
        }
    }
}
