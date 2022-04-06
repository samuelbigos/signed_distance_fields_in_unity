using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SDF
{
    public class SDFGen : MonoBehaviour
    {
        [SerializeField] private ComputeShader _sdfGenCompute;
        [SerializeField] private ComputeShader _sdfMutateCompute;
        [SerializeField] private Vector3Int _sdfResolution;
        [SerializeField] private float _sdfRadius;
        [SerializeField] private float _sphereRadius;
        [SerializeField] private float _sdfMaxDist;
        [SerializeField] private SDFRenderer _sdfRendererInstance;
        [SerializeField] private Camera _mainCamera;

        public float SDFRadius => _sdfRadius;
        public RenderTexture SDFTexture => _sdfVolumeTexture[_activeSdfTex];

        private int _activeSdfTex = 0;
        private readonly RenderTexture[] _sdfVolumeTexture = new RenderTexture[2];
        private Vector3Int _numKernels;
        private uint _kernelSizeX;
        private uint _kernelSizeY;
        private uint _kernelSizeZ;
        
        private void Start()
        {
            SetComputeSDFParams(_sdfMutateCompute);
            SetComputeSDFParams(_sdfGenCompute);                         
            SetMaterialSDFParams(_sdfRendererInstance.SDFRendererMat);   
            InitGenCompute();
        }

        private void Update()
        {
            //if (Mouse.current.middleButton.wasReleasedThisFrame)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                _sdfMutateCompute.SetBool("_mouseAdd", Mouse.current.leftButton.isPressed);
                _sdfMutateCompute.SetBool("_mouseSubtract", Keyboard.current.xKey.isPressed);
                _sdfMutateCompute.SetVector("_mouseOrigin", ray.origin);
                _sdfMutateCompute.SetVector("_mouseDir", ray.direction);
        
                int inSdfTex = _activeSdfTex;
                int outSdfTex = (_activeSdfTex + 1) & 2;
        
                int kernelID = _sdfMutateCompute.FindKernel("SDFMutate");
                _sdfMutateCompute.SetTexture(kernelID, "_sdfTexIn", _sdfVolumeTexture[inSdfTex], 0, RenderTextureSubElement.Color);
                _sdfMutateCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[outSdfTex], 0, RenderTextureSubElement.Color);

                _sdfMutateCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);
        
                _activeSdfTex = outSdfTex;
                _sdfRendererInstance.SetSDFTexture(SDFTexture);
            }
        }

        private void InitGenCompute()
        {
            int kernelID = _sdfGenCompute.FindKernel("SDFGen");
            _sdfGenCompute.GetKernelThreadGroupSizes(kernelID, out _kernelSizeX, out _kernelSizeY, out _kernelSizeZ);
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
            
             _sdfVolumeTexture[0].filterMode = FilterMode.Bilinear;
             _sdfVolumeTexture[1].filterMode = FilterMode.Bilinear;
            
            _sdfGenCompute.SetFloat("_sphereRadius", _sphereRadius);
            _sdfGenCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[0], 0, RenderTextureSubElement.Color);

            _sdfGenCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);
            
            _sdfRendererInstance.SetSDFTexture(_sdfVolumeTexture[0]);
        }

        public void SetComputeSDFParams(ComputeShader shader)
        {
            shader.SetInt("_sdfTexSizeX", _sdfResolution.x);
            shader.SetInt("_sdfTexSizeY", _sdfResolution.y);
            shader.SetInt("_sdfTexSizeZ", _sdfResolution.z);
            shader.SetFloat("_sdfRadius", _sdfRadius);
            shader.SetFloat("_sdfMaxDist", _sdfMaxDist);
            shader.SetFloat("_planetRadius", _sphereRadius);
        }
        
        public void SetMaterialSDFParams(Material mat)
        {
            mat.SetInt("_sdfTexSizeX", _sdfResolution.x);
            mat.SetInt("_sdfTexSizeY", _sdfResolution.y);
            mat.SetInt("_sdfTexSizeZ", _sdfResolution.z);
            mat.SetFloat("_sdfRadius", _sdfRadius);
            mat.SetFloat("_sdfMaxDist", _sdfMaxDist);
            mat.SetFloat("_planetRadius", _sphereRadius);
        }
    }
}
