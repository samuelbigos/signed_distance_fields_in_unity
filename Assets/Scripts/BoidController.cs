using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoidController : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public float radius;
    }

    public Vector3 PlanetCentre = new Vector3(0.0f, 0.0f, 0.0f);
    public float PlanetGravity = 1.0f;
    public SDFRenderer SDFRendererInstance;
    public SDFGen SDFGenInstance;
    public ComputeShader BoidPhysicsCompute;    

    List<Boid> _boids = new List<Boid>();

    void Start(){}

    void PhysicsCompute()
    {
        int kernelID = BoidPhysicsCompute.FindKernel("Step");
        uint kernelSizeX;
        uint kernelSizeY;
        uint kernelSizeZ;
        BoidPhysicsCompute.GetKernelThreadGroupSizes(kernelID, out kernelSizeX, out kernelSizeY, out kernelSizeZ);

        BoidPhysicsCompute.SetInt("_numBoids", _boids.Count);

        ComputeBuffer boidBufferIn = new ComputeBuffer(_boids.Count, 7 * 4);
        boidBufferIn.SetData(_boids);
        BoidPhysicsCompute.SetBuffer(kernelID, "_boidBufferIn", boidBufferIn);

        ComputeBuffer boidBufferOut = new ComputeBuffer(_boids.Count, 7 * 4);
        boidBufferOut.SetData(_boids);
        BoidPhysicsCompute.SetBuffer(kernelID, "_boidBufferOut", boidBufferOut);

        BoidPhysicsCompute.SetVector("_planetCentre", PlanetCentre);
        BoidPhysicsCompute.SetFloat("_timeStep", Time.deltaTime);
        BoidPhysicsCompute.SetFloat("_gravity", PlanetGravity);

        BoidPhysicsCompute.SetTexture(kernelID, "_sdfTex", SDFGenInstance.GetSDFTexture());
        BoidPhysicsCompute.SetFloat("_sdfRadius", SDFGenInstance.SDFRadius);
        BoidPhysicsCompute.SetFloat("_sdfDistMod", SDFGenInstance.SDFDistMod);

        BoidPhysicsCompute.Dispatch(kernelID, _boids.Count, 1, 1);

        Boid[] boidArray = new Boid[_boids.Count];
        boidBufferOut.GetData(boidArray);

        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i] = boidArray[i];
        }

        boidBufferIn.Dispose();
        boidBufferOut.Dispose();
    }

    void Update()
    {
        if (_boids.Count > 0)
        {
            PhysicsCompute();
        }
        SDFRendererInstance.SetBoids(_boids);
    }

    public void Spawn(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Started)
            return;

        Boid newBoid = new Boid();

        newBoid.position = new Vector4(0.0f, 1.8f, 0.0f);
        newBoid.velocity = new Vector4(PlanetGravity * 0.2f * Random.Range(-1.0f, 1.0f), 0.0f, PlanetGravity * 0.2f * Random.Range(-1.0f, 1.0f));
        newBoid.radius = 0.066f;
        _boids.Add(newBoid);
    }
}
