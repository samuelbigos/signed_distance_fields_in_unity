using UImGui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SDF
{
    public class SDF : MonoBehaviour
    {
        [SerializeField] private ComputeShader SDFCompute;
        [SerializeField] private Vector3Int SDFResolution;
        [SerializeField] private float SDFRadius;
        [SerializeField] private float SphereRadius;
        [SerializeField] private float SDFMaxDist;
        [SerializeField] private SDFRenderer SDFRendererInstance;
        [SerializeField] private Camera MainCamera;
        [SerializeField] private GameObject OrbitCam;
        [SerializeField] private GameObject WASDCam;

        public RenderTexture SDFTexture => _sdfVolumeTexture[_activeSdfTex];

        private int _activeSdfTex;
        private readonly RenderTexture[] _sdfVolumeTexture = new RenderTexture[2];
        private Vector3Int _numKernels;
        private uint _kernelSizeX;
        private uint _kernelSizeY;
        private uint _kernelSizeZ;
        private bool _reset = true;
        private int _eikonelPasses;
        private int _lastCamMode;
        private static readonly int SDF_TEX_SIZE_X = Shader.PropertyToID("_sdfTexSizeX");
        private static readonly int SDF_TEX_SIZE_Y = Shader.PropertyToID("_sdfTexSizeY");
        private static readonly int SDF_TEX_SIZE_Z = Shader.PropertyToID("_sdfTexSizeZ");
        private static readonly int RADIUS = Shader.PropertyToID("_sdfRadius");
        private static readonly int MAX_DIST = Shader.PropertyToID("_sdfMaxDist");
        private static readonly int SPHERE_RADIUS1 = Shader.PropertyToID("_sphereRadius");
        private static readonly int MAX_RAYMARCH_STEPS = Shader.PropertyToID("_maxRaymarchSteps");
        private static readonly int SUN_POS = Shader.PropertyToID("_sunPos");
        private static readonly int SHADOW_INTENSITY = Shader.PropertyToID("_shadowIntensity");
        private static readonly int SHADOW_SOFTNESS = Shader.PropertyToID("_shadowSoftness");

        private void Start()
        {
            _lastCamMode = DebugWindow.Instance.CameraMode;
        }

        private void Update()
        {
            SDFResolution.x = DebugWindow.Instance.SdfResolution * 8;
            SDFResolution.y = DebugWindow.Instance.SdfResolution * 8;
            SDFResolution.z = DebugWindow.Instance.SdfResolution * 8;
            
            SetSDFParams(SDFCompute);                       
            SetSDFParams(SDFRendererInstance.SDFRendererMat);
            
            if (_reset)
            {
                InitGenCompute();
                _reset = false;
                return;
            }
            
            Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            SDFCompute.SetBool("_mouseAdd", Mouse.current.leftButton.isPressed && DebugWindow.Instance.Mode == 0);
            SDFCompute.SetBool("_mouseSubtract", Mouse.current.leftButton.isPressed && DebugWindow.Instance.Mode == 1);
            SDFCompute.SetVector("_mouseOrigin", ray.origin);
            SDFCompute.SetVector("_mouseDir", ray.direction);
    
            int inSdfTex = _activeSdfTex;
            int outSdfTex = (_activeSdfTex + 1) & 2;
    
            int kernelID = SDFCompute.FindKernel("SDFMutate");
            SDFCompute.SetTexture(kernelID, "_sdfTexIn", _sdfVolumeTexture[inSdfTex], 0, RenderTextureSubElement.Color);
            SDFCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[outSdfTex], 0, RenderTextureSubElement.Color);

            SDFCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);
    
            _activeSdfTex = outSdfTex;
            SDFRendererInstance.SetSDFTexture(SDFTexture);

            _eikonelPasses = Mathf.Max(_eikonelPasses - 1, 0);
            if (Mouse.current.leftButton.isPressed && DebugWindow.Instance.Mode == 1)
            {
                // Do enough passes to fill in twice the radius of a cut.
                _eikonelPasses = (int)(DebugWindow.Instance.BrushSize / (SDFRadius * 2.0 / SDFResolution.x)) * 2;
            }
            
            if (DebugWindow.Instance.CameraMode != _lastCamMode)
            {
                _lastCamMode = DebugWindow.Instance.CameraMode;
                OrbitCam.SetActive(_lastCamMode == 0);
                WASDCam.SetActive(_lastCamMode == 1);

                if (_lastCamMode == 1)
                {
                    WASDCam.transform.position = OrbitCam.transform.position;
                    WASDCam.transform.rotation = OrbitCam.transform.rotation;
                }
            }
        }

        public void Reset()
        {
            _reset = true;
        }
        
        private void InitGenCompute()
        {
            int kernelID = SDFCompute.FindKernel("SDFGen");
            SDFCompute.GetKernelThreadGroupSizes(kernelID, out _kernelSizeX, out _kernelSizeY, out _kernelSizeZ);
            if (SDFResolution.x % _kernelSizeX != 0 ||
                SDFResolution.y % _kernelSizeY != 0 ||
                SDFResolution.z % _kernelSizeZ != 0)
            {
                Debug.LogError("SDF kernel size must be a multiple of SDF resolution.");
            }

            _numKernels.x = (int)(SDFResolution.x / _kernelSizeX);
            _numKernels.y = (int)(SDFResolution.y / _kernelSizeY);
            _numKernels.z = (int)(SDFResolution.z / _kernelSizeZ);

            // Create output 3D render texture.
            RenderTextureDescriptor renderTexDesc = new RenderTextureDescriptor();
            renderTexDesc.dimension = TextureDimension.Tex3D;
            renderTexDesc.colorFormat = RenderTextureFormat.RFloat;
            renderTexDesc.enableRandomWrite = true;
            renderTexDesc.width = SDFResolution.x;
            renderTexDesc.height = SDFResolution.y;
            renderTexDesc.volumeDepth = SDFResolution.z;
            renderTexDesc.msaaSamples = 1;
            renderTexDesc.mipCount = 0;
            renderTexDesc.useMipMap = false;

            _sdfVolumeTexture[0] = new RenderTexture(renderTexDesc);
            _sdfVolumeTexture[0].Create();
            _sdfVolumeTexture[1] = new RenderTexture(renderTexDesc);
            _sdfVolumeTexture[1].Create();

            SDFCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[0], 0, RenderTextureSubElement.Color);

            SDFCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);

            SDFRendererInstance.SetSDFTexture(_sdfVolumeTexture[0]);
        }

        public void SetSDFParams(ComputeShader shader)
        {
            shader.SetInt("_sdfTexSizeX", SDFResolution.x);
            shader.SetInt("_sdfTexSizeY", SDFResolution.y);
            shader.SetInt("_sdfTexSizeZ", SDFResolution.z);
            shader.SetFloat("_sdfRadius", SDFRadius);
            shader.SetFloat("_sdfMaxDist", SDFMaxDist);
            shader.SetFloat("_sphereRadius", SphereRadius);
            shader.SetFloat("_brushSize", DebugWindow.Instance.BrushSize);
            shader.SetFloat("_brushHeight", DebugWindow.Instance.BrushHeight);
            shader.SetBool("_doEikonel", _eikonelPasses > 0);
            shader.SetInt("_maxRaymarchSteps", DebugWindow.Instance.MaxRaymarchSteps);
        }
        
        public void SetSDFParams(Material mat)
        {
            mat.SetInt(SDF_TEX_SIZE_X, SDFResolution.x);
            mat.SetInt(SDF_TEX_SIZE_Y, SDFResolution.y);
            mat.SetInt(SDF_TEX_SIZE_Z, SDFResolution.z);
            mat.SetFloat(RADIUS, SDFRadius);
            mat.SetFloat(MAX_DIST, SDFMaxDist);
            mat.SetFloat(SPHERE_RADIUS1, SphereRadius);
            mat.SetInt(MAX_RAYMARCH_STEPS, DebugWindow.Instance.MaxRaymarchSteps);
            mat.SetVector(SUN_POS, DebugWindow.Instance.SunPos);
            mat.SetFloat(SHADOW_INTENSITY, DebugWindow.Instance.ShadowIntensity);
            mat.SetFloat(SHADOW_SOFTNESS, DebugWindow.Instance.ShadowSoftness);
        }
    }
}
