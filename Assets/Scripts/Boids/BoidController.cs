using System.Collections.Generic;
using SDF;
using UImGui;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [SerializeField] private Vector3 PlanetCentre = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField] private SDFRenderer SDFRendererInstance;
        [SerializeField] private SDF.SDF SDFInstance;
        [SerializeField] private ComputeShader BoidPhysicsCompute;
        [SerializeField] private DebugWindow DebugWindow;

        private readonly List<Boid> _boids = new List<Boid>();
        private int _playerBoidID;

        private void Start()
        {
            BoidPhysicsCompute.SetVector("_planetCentre", PlanetCentre);
            BoidPhysicsCompute.SetFloat("_gravity", 1.0f);
        }

        private void PhysicsCompute()
        {
            int kernelID = BoidPhysicsCompute.FindKernel("Step");
            BoidPhysicsCompute.GetKernelThreadGroupSizes(kernelID, out uint _, out uint _, out uint _);

            BoidPhysicsCompute.SetInt("_numBoids", _boids.Count);

            ComputeBuffer boidBufferIn = new ComputeBuffer(_boids.Count, 7 * 4);
            boidBufferIn.SetData(_boids);
            BoidPhysicsCompute.SetBuffer(kernelID, "_boidBufferIn", boidBufferIn);

            ComputeBuffer boidBufferOut = new ComputeBuffer(_boids.Count, 7 * 4);
            boidBufferOut.SetData(_boids);
            BoidPhysicsCompute.SetBuffer(kernelID, "_boidBufferOut", boidBufferOut);

            BoidPhysicsCompute.SetFloat("_timeStep", Time.deltaTime);
            BoidPhysicsCompute.SetFloat("_gravity", DebugWindow.Gravity);
            BoidPhysicsCompute.SetTexture(kernelID, "_sdfTexIn", SDFInstance.SDFTexture);

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

        private void Update()
        {
            SDFInstance.SetSDFParams(BoidPhysicsCompute);
            
            if (_boids.Count > 0)
            {
                PhysicsCompute();
            }
            SDFRendererInstance.SetBoids(_boids);

            if (DebugWindow.Spawn)
            {
                DebugWindow.Spawn = false;
                DoSpawn();
            }

            if (DebugWindow.Reset)
            {
                DebugWindow.Reset = false;
                _boids.Clear();
                SDFInstance.Reset();
            }
        }

        public void Spawn(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Started)
                return;
            
            DoSpawn();
        }

        public void DoSpawn()
        {
            float impulse = 0.25f;
            Boid newBoid = new()
            {
                Position = new Vector4(0.0f, 1.5f, 0.0f),
                Velocity = new Vector4(DebugWindow.Gravity * impulse * Random.Range(-1.0f, 1.0f), 0.0f, DebugWindow.Gravity * impulse * Random.Range(-1.0f, 1.0f)),
                Radius = 0.066f
            };
            
            _boids.Add(newBoid);
        }
    }
}
