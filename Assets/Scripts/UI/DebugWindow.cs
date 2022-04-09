using System;
using ImGuiNET;
#if !UIMGUI_REMOVE_IMNODES
using imnodesNET;
#endif
#if !UIMGUI_REMOVE_IMPLOT
using ImPlotNET;
using System.Linq;
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
using ImGuizmoNET;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

namespace UImGui
{
    public class DebugWindow : MonoBehaviour
    {
        private static DebugWindow _instance;
        public static DebugWindow Instance => _instance;
        
        private int _mode = 0;
        private float _brushSize = 0.1f;
        private float _brushHeight = 0.0f;
        private bool _reset = false;
        private bool _spawn = false;
        private float _gravity = 1.0f;
        private int _debugDrawMode = 0;
        private int _cameraMode = 0;
        private int _sdfResolution = 32;
        private float _fps;
        private int _aoSamples = 8;
        private float _aoKernelSize = 0.075f;
        private int _maxRaymarchSteps = 128;
        
        public int Mode => _mode;
        public float BrushSize => _brushSize;
        public float BrushHeight => _brushHeight;
        public bool Reset
        {
            get => _reset;
            set => _reset = value;
        }
        public bool Spawn
        {
            get => _spawn;
            set => _spawn = value;
        }
        public float Gravity => _gravity;
        public int DebugDrawMode => _debugDrawMode;
        public int CameraMode => _cameraMode;
        public int SDFResolution => _sdfResolution * 8;
        public int AOSamples => _aoSamples;
        public float AOKernelSize => _aoKernelSize;
        public int MaxRaymarchSteps => _maxRaymarchSteps;

        private void Awake()
        {
            _instance = this;
        }

        private void OnEnable()
        {
            UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGuiUtility.Layout -= OnLayout;
        }

        private void Update()
        {
            float fps = 1.0f / Time.deltaTime;
            _fps = 0.033f * fps + 0.966f * _fps;
        }

        private void OnLayout(UImGui uImGui)
        {
            if (ImGui.Begin("DEBUG MENU", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"FPS: {_fps:F0}");
                
                ImGui.BeginTabBar("DEBUG MENU#left_tabs_bar");
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    _spawn = ImGui.Button("Spawn Boid");
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.Text("BRUSH SIZE:");
                    ImGui.SliderFloat("##1", ref _brushSize, 0.005f, 0.25f);
                    ImGui.Text("BRUSH HEIGHT:");
                    ImGui.SliderFloat("##2", ref _brushHeight, -1.0f, 1.0f);
                    ImGui.Text("GRAVITY:");
                    ImGui.SliderFloat("##3", ref _gravity, -2.0f, 2.0f);

                    ImGui.Dummy(Vector2.one * 5.0f);

                    ImGui.Text("SDF MODIFY MODE:");
                    ImGui.RadioButton("Add", ref _mode, 0); ImGui.SameLine();
                    ImGui.RadioButton("Cut", ref _mode, 1); ImGui.SameLine();
                    ImGui.RadioButton("None", ref _mode, 2);
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.Text("CAMERA MODE:");
                    ImGui.RadioButton("Orbit", ref _cameraMode, 0); ImGui.SameLine();
                    ImGui.RadioButton("WASD", ref _cameraMode, 1);
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.Text("DRAW MODE:");
                    ImGui.RadioButton("Standard", ref _debugDrawMode, 0); ImGui.SameLine();
                    ImGui.RadioButton("Debug", ref _debugDrawMode, 1);
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.Text($"SDF RESOLUTION ({_sdfResolution * 8}^3)");
                    ImGui.SliderInt("##4", ref _sdfResolution, 2, 64);
                    _reset = ImGui.Button("RESET");
                    ImGui.Dummy(Vector2.one * 5.0f);
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Rendering"))
                {
                    ImGui.Text("AO SAMPLES:");
                    ImGui.SliderInt("##1", ref _aoSamples, 0, 32);
                    
                    ImGui.Text("AO KERNEL SIZE:");
                    ImGui.SliderFloat("##2", ref _aoKernelSize, 0.0f, 0.25f);
                    
                    ImGui.Text("MAX RAYMARCH STEPS:");
                    ImGui.SliderInt("##3", ref (_maxRaymarchSteps), 8, 512);
                    
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.End();
            }
        }
    }
}