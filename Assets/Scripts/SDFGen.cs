using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

public class SDFGen : MonoBehaviour
{
    public ComputeShader SDFGenCompute;
    public ComputeShader SDFMutateCompute;
    public Vector3Int SDFResolution;
    public float SDFRadius;
    public float SDFDistMod;
    public float SphereRadius;
    public SDFRenderer SDFRendererInstance;
    public Camera MainCamera;

    private int _activeSdfTex = 0;
    private RenderTexture[] _sdfVolumeTexture = new RenderTexture[2];
    private Vector3Int _numKernels;
    private uint _kernelSizeX;
    private uint _kernelSizeY;
    private uint _kernelSizeZ;

    public RenderTexture GetSDFTexture()
    {
        return _sdfVolumeTexture[_activeSdfTex];
    }

    void InitGenCompute()
    {
        int kernelID = SDFGenCompute.FindKernel("SDFGen");
        SDFGenCompute.GetKernelThreadGroupSizes(kernelID, out _kernelSizeX, out _kernelSizeY, out _kernelSizeZ);
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

        _sdfVolumeTexture[0] = new RenderTexture(renderTexDesc);
        _sdfVolumeTexture[0].Create();
        _sdfVolumeTexture[1] = new RenderTexture(renderTexDesc);
        _sdfVolumeTexture[1].Create();

        SDFGenCompute.SetInt("_sdfTexSizeX", SDFResolution.x);
        SDFGenCompute.SetInt("_sdfTexSizeY", SDFResolution.y);
        SDFGenCompute.SetInt("_sdfTexSizeZ", SDFResolution.z);
        SDFGenCompute.SetFloat("_sdfRadius", SDFRadius);
        SDFGenCompute.SetFloat("_sdfDistMod", SDFDistMod);
        SDFGenCompute.SetFloat("_sphereRadius", SphereRadius);
        SDFGenCompute.SetTexture(kernelID, "_sdfTex", _sdfVolumeTexture[0], 0, RenderTextureSubElement.Color);

        SDFRendererInstance.SetSDFParams(SDFDistMod, SDFRadius, SphereRadius);

        SDFGenCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);
        SDFRendererInstance.SetSDFTexture(_sdfVolumeTexture[0]);
    }

    void InitMutateCompute()
    {
        SDFMutateCompute.SetInt("_sdfTexSizeX", SDFResolution.x);
        SDFMutateCompute.SetInt("_sdfTexSizeY", SDFResolution.y);
        SDFMutateCompute.SetInt("_sdfTexSizeZ", SDFResolution.z);
        SDFMutateCompute.SetFloat("_sdfRadius", SDFRadius);
        SDFMutateCompute.SetFloat("_sdfDistMod", SDFDistMod);
    }

    void Start()
    {
        InitGenCompute();
        InitMutateCompute();
    }

    void Update()
    {
        Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        SDFMutateCompute.SetBool("_mouseAdd", Mouse.current.leftButton.isPressed);
        SDFMutateCompute.SetBool("_mouseSubtract", Keyboard.current.xKey.isPressed);
        SDFMutateCompute.SetVector("_mouseOrigin", ray.origin);
        SDFMutateCompute.SetVector("_mouseDir", ray.direction);

        int inSdfTex = _activeSdfTex;
        int outSdfTex = (_activeSdfTex + 1) & 2;

        int kernelID = SDFMutateCompute.FindKernel("SDFMutate");
        SDFMutateCompute.SetTexture(kernelID, "_sdfTexIn", _sdfVolumeTexture[inSdfTex], 0, RenderTextureSubElement.Color);
        SDFMutateCompute.SetTexture(kernelID, "_sdfTexOut", _sdfVolumeTexture[outSdfTex], 0, RenderTextureSubElement.Color);
        SDFMutateCompute.Dispatch(kernelID, _numKernels.x, _numKernels.y, _numKernels.z);

        _activeSdfTex = outSdfTex;

        SDFRendererInstance.SetSDFTexture(GetSDFTexture());
    }
}
