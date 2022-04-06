using System;
using System.Collections.Generic;
using SDF;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Boids
{
    public class BoidController : MonoBehaviour
    {
        public struct Boid
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Radius;
        }

        [SerializeField] private Vector3 _planetCentre = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField] private SDFRenderer _sdfRendererInstance;
        [SerializeField] private SDFGen _sdfGenInstance;
        [SerializeField] private ComputeShader _boidPhysicsCompute;

        private readonly List<Boid> _boids = new List<Boid>();
        private int _playerBoidID;

        private void Start()
        {
            _boidPhysicsCompute.SetVector("_planetCentre", _planetCentre);
            _boidPhysicsCompute.SetFloat("_gravity", SDFSurface.Instance.Gravity);
            _sdfGenInstance.SetComputeSDFParams(_boidPhysicsCompute);
        }

        private void PhysicsCompute()
        {
            int kernelID = _boidPhysicsCompute.FindKernel("Step");
            _boidPhysicsCompute.GetKernelThreadGroupSizes(kernelID, out uint _, out uint _, out uint _);

            _boidPhysicsCompute.SetInt("_numBoids", _boids.Count);

            ComputeBuffer boidBufferIn = new ComputeBuffer(_boids.Count, 7 * 4);
            boidBufferIn.SetData(_boids);
            _boidPhysicsCompute.SetBuffer(kernelID, "_boidBufferIn", boidBufferIn);

            ComputeBuffer boidBufferOut = new ComputeBuffer(_boids.Count, 7 * 4);
            boidBufferOut.SetData(_boids);
            _boidPhysicsCompute.SetBuffer(kernelID, "_boidBufferOut", boidBufferOut);

            _boidPhysicsCompute.SetFloat("_timeStep", Time.deltaTime);
            _boidPhysicsCompute.SetTexture(kernelID, "_sdfTexIn", _sdfGenInstance.SDFTexture);

            _boidPhysicsCompute.Dispatch(kernelID, _boids.Count, 1, 1);

            Boid[] boidArray = new Boid[_boids.Count];
            boidBufferOut.GetData(boidArray);

            for (int i = 0; i < _boids.Count; i++)
            {
                _boids[i] = boidArray[i];
            }

            boidBufferIn.Dispose();
            boidBufferOut.Dispose();
        }

        private void Update()
        {
            if (_boids.Count > 0)
            {
                PhysicsCompute();
            }
            _sdfRendererInstance.SetBoids(_boids);
        }

        public void Spawn(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Started)
                return;

            float impulse = 0.1f;
            Boid newBoid = new()
            {
                Position = new Vector4(0.0f, 1.5f, 0.0f),
                Velocity = new Vector4(SDFSurface.Instance.Gravity * impulse * Random.Range(-1.0f, 1.0f), 0.0f, SDFSurface.Instance.Gravity * impulse * Random.Range(-1.0f, 1.0f)),
                Radius = 0.066f
            };

            _boids.Add(newBoid);
        }
    }
}
