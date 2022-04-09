using System;
using UImGui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace SDF
{
    public class SDF : MonoBehaviour
    {
        [SerializeField] private ComputeShader _sdfCompute;
        [SerializeField] private Vector3Int _sdfResolution;
        [SerializeField] private float _sdfRadius;
        [SerializeField] private float _sphereRadius;
        [SerializeField] private float _sdfMaxDist;
        [SerializeField] private SDFRenderer _sdfRendererInstance;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GameObject _orbitCam;
        [SerializeField] private GameObject _wasdCam;

        public RenderTexture SDFTexture => _sdfVolumeTexture[_activeSdfTex];

        private int _activeSdfTex = 0;
        private readonly RenderTexture[] _sdfVolumeTexture = new RenderTexture[2];
        private Vector3Int _numKernels;
        private uint _kernelSizeX;
        private uint _kernelSizeY;
        private uint _kernelSizeZ;
        private bool _reset = true;
        private int _eikonelPasses;
        private int _lastCamMode;

        private void Start()
        {
            _lastCamMode = DebugWindow.Instance.CameraMode;
        }

        private void Update()
        {
            _sdfResolution.x = DebugWindow.Instance.SdfResolution * 8;
            _sdfResolution.y = DebugWindow.Instance.SdfResolution * 8;
            _sdfResolution.z = DebugWindow.Instance.SdfResolution * 8;
            
            SetSDFParams(_sdfCompute);                       
            SetSDFParams(_sdfRendererInstance.SDFRendererMat);
            
            if (_reset)
            {
                InitGenCompute();
                _reset = false;
                return;
            }
            
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            _sdfCompute.SetBool("_mouseAdd", Mouse.current.leftButton.isPressed && DebugWindow.Instance.Mode == 0);
            _sdfCompute.SetBool("_mouseSubtract", Mouse.current.leftButton.isPressed && DebugWindow.Instance.Mode == 1);
            _sdfCompute.SetVector("_mouseOrigin", ray.origin);
            _sdfCompute.SetVector("_mouseDir", ray.direction);
    
            int inSdfTex = _activeSdfTex;
            int outSdfTex = (_activeSdfTex + 1) & 2;
    
            int kernelID = _sdfCompute.FindKernel("SDFMutate");
            _sdfCompute.SetTexture(kernelID, "_sdfTexIn", _sdfVolumeTexture[inSdfTex], 0, RenderTextureSubElement.Color);
            _sdfCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[outSdfTex], 0, RenderTextureSubElement.Color);

            _sdfCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);
    
            _activeSdfTex = outSdfTex;
            _sdfRendererInstance.SetSDFTexture(SDFTexture);

            _eikonelPasses = Mathf.Max(_eikonelPasses - 1, 0);
            if (Mouse.current.leftButton.isPressed && DebugWindow.Instance.Mode == 1)
            {
                // Do enough passes to fill in twice the radius of a cut.
                _eikonelPasses = (int)(DebugWindow.Instance.BrushSize / (_sdfRadius * 2.0 / _sdfResolution.x)) * 2;
            }
            
            if (DebugWindow.Instance.CameraMode != _lastCamMode)
            {
                _lastCamMode = DebugWindow.Instance.CameraMode;
                _orbitCam.SetActive(_lastCamMode == 0);
                _wasdCam.SetActive(_lastCamMode == 1);

                if (_lastCamMode == 1)
                {
                    _wasdCam.transform.position = _orbitCam.transform.position;
                    _wasdCam.transform.rotation = _orbitCam.transform.rotation;
                }
            }
        }

        public void Reset()
        {
            _reset = true;
        }
        
        private void InitGenCompute()
        {
            int kernelID = _sdfCompute.FindKernel("SDFGen");
            _sdfCompute.GetKernelThreadGroupSizes(kernelID, out _kernelSizeX, out _kernelSizeY, out _kernelSizeZ);
            if (_sdfResolution.x % _kernelSizeX != 0 ||
                _sdfResolution.y % _kernelSizeY != 0 ||
                _sdfResolution.z % _kernelSizeZ != 0)
            {
                Debug.LogError("SDF kernel size must be a multiple of SDF resolution.");
            }

            _numKernels.x = (int)(_sdfResolution.x / _kernelSizeX);
            _numKernels.y = (int)(_sdfResolution.y / _kernelSizeY);
            _numKernels.z = (int)(_sdfResolution.z / _kernelSizeZ);

            // Create output 3D render texture.
            RenderTextureDescriptor renderTexDesc = new RenderTextureDescriptor();
            renderTexDesc.dimension = TextureDimension.Tex3D;
            renderTexDesc.colorFormat = RenderTextureFormat.RFloat;
            renderTexDesc.enableRandomWrite = true;
            renderTexDesc.width = _sdfResolution.x;
            renderTexDesc.height = _sdfResolution.y;
            renderTexDesc.volumeDepth = _sdfResolution.z;
            renderTexDesc.msaaSamples = 1;
            renderTexDesc.mipCount = 0;
            renderTexDesc.useMipMap = false;

            _sdfVolumeTexture[0] = new RenderTexture(renderTexDesc);
            _sdfVolumeTexture[0].Create();
            _sdfVolumeTexture[1] = new RenderTexture(renderTexDesc);
            _sdfVolumeTexture[1].Create();

            _sdfCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[0], 0, RenderTextureSubElement.Color);

            _sdfCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);

            _sdfRendererInstance.SetSDFTexture(_sdfVolumeTexture[0]);
        }

        public void SetSDFParams(ComputeShader shader)
        {
            shader.SetInt("_sdfTexSizeX", _sdfResolution.x);
            shader.SetInt("_sdfTexSizeY", _sdfResolution.y);
            shader.SetInt("_sdfTexSizeZ", _sdfResolution.z);
            shader.SetFloat("_sdfRadius", _sdfRadius);
            shader.SetFloat("_sdfMaxDist", _sdfMaxDist);
            shader.SetFloat("_sphereRadius", _sphereRadius);
            shader.SetFloat("_brushSize", DebugWindow.Instance.BrushSize);
            shader.SetFloat("_brushHeight", DebugWindow.Instance.BrushHeight);
            shader.SetBool("_doEikonel", _eikonelPasses > 0);
            shader.SetInt("_maxRaymarchSteps", DebugWindow.Instance.MaxRaymarchSteps);
        }
        
        public void SetSDFParams(Material mat)
        {
            mat.SetInt("_sdfTexSizeX", _sdfResolution.x);
            mat.SetInt("_sdfTexSizeY", _sdfResolution.y);
            mat.SetInt("_sdfTexSizeZ", _sdfResolution.z);
            mat.SetFloat("_sdfRadius", _sdfRadius);
            mat.SetFloat("_sdfMaxDist", _sdfMaxDist);
            mat.SetFloat("_sphereRadius", _sphereRadius);
            mat.SetInt("_maxRaymarchSteps", DebugWindow.Instance.MaxRaymarchSteps);
            mat.SetVector("_sunPos", DebugWindow.Instance.SunPos);
            mat.SetFloat("_shadowIntensity", DebugWindow.Instance.ShadowIntensity);
            mat.SetFloat("_shadowSoftness", DebugWindow.Instance.ShadowSoftness);
        }
    }
}
