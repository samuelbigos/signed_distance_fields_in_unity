using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFRenderer : MonoBehaviour
{
    Material _sdfRendererMat;
    public Texture2D _planetTexture;

    private void Awake()
    {
    }

    void Start()
    {
        _sdfRendererMat = GetComponent<MeshRenderer>().material;
        _sdfRendererMat.SetVectorArray("_boidPositions", new Vector4[256]);
        _sdfRendererMat.SetFloatArray("_boidRadii", new float[256]);
        _sdfRendererMat.SetTexture("_planetTex", _planetTexture);
        _sdfRendererMat.SetFloat("_planetTexHeight", (float)_planetTexture.height);
    }

    public void SetSDFParams(float distMod, float sdfRadius, float planetRadius)
    {
        _sdfRendererMat.SetFloat("_sdfDistMod", distMod);
        _sdfRendererMat.SetFloat("_sdfRadius", sdfRadius);
        _sdfRendererMat.SetFloat("_planetRadius", planetRadius);
    }

    public void SetSDFTexture(RenderTexture tex)
    {
        _sdfRendererMat.SetTexture("_sdfTexture", tex);
    }

    public void SetBoids(List<BoidController.Boid> boids)
    {
        _sdfRendererMat.SetFloat("_boidCount", boids.Count);
        if (boids.Count > 0)
        {
            Vector4[] posArray = new Vector4[boids.Count];
            float[] radArray = new float[boids.Count];
            for (int i = 0; i <boids.Count; i++)
            {
                posArray[i] = boids[i].position;
                radArray[i] = boids[i].radius;
            }
            _sdfRendererMat.SetVectorArray("_boidPositions", posArray);
            _sdfRendererMat.SetFloatArray("_boidRadii", radArray);
        }
    }
}
