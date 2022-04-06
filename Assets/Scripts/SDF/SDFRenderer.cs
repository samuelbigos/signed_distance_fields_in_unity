using System.Collections.Generic;
using Boids;
using UnityEngine;

namespace SDF
{
    public class SDFRenderer : MonoBehaviour
    {
        public Material SDFRendererMat => _sdfRendererMat;
        
        private Material _sdfRendererMat;
        [SerializeField] private Texture2D _planetTexture;
        
        private static readonly int _boidCount = Shader.PropertyToID("_boidCount");
        private static readonly int _boidPositions = Shader.PropertyToID("_boidPositions");
        private static readonly int _boidRadii = Shader.PropertyToID("_boidRadii");

        private void Start()
        {
            _sdfRendererMat = GetComponent<MeshRenderer>().material;
            _sdfRendererMat.SetVectorArray("_boidPositions", new Vector4[256]);
            _sdfRendererMat.SetFloatArray("_boidRadii", new float[256]);
            _sdfRendererMat.SetTexture("_planetTex", _planetTexture);
            _sdfRendererMat.SetFloat("_planetTexHeight", (float)_planetTexture.height);
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
