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
        
        public int Mode = 0;
        public float BrushSize = 0.1f;
        public float BrushHeight = 0.0f;
        public bool Reset = false;
        public bool Spawn = false;
        public float Gravity = 1.0f;
        public int DebugDrawMode = 0;
        public int CameraMode = 0;
        public int SdfResolution = 32;
        public float FPS;
        public int AOSamples = 8;
        public float AOKernelSize = 0.075f;
        public int MaxRaymarchSteps = 128;
        public Vector3 SunPos = new Vector3(0.0f, 10.0f, 0.0f);
        public float ShadowIntensity = 0.5f;
        public float ShadowSoftness = 0.8f;
        
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
            FPS = 0.033f * fps + 0.966f * FPS;
        }

        private void OnLayout(UImGui uImGui)
        {
            if (ImGui.Begin("DEBUG MENU", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"FPS: {FPS:F0}");
                
                ImGui.BeginTabBar("DEBUG MENU#left_tabs_bar");
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    Spawn = ImGui.Button("Spawn Boid");
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.TextColored(Color.gray, "SDF Modify Mode:");
                    ImGui.RadioButton("Add", ref Mode, 0); ImGui.SameLine();
                    ImGui.RadioButton("Cut", ref Mode, 1); ImGui.SameLine();
                    ImGui.RadioButton("None", ref Mode, 2);
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.TextColored(Color.gray, "Camera Mode:");
                    ImGui.RadioButton("Orbit", ref CameraMode, 0); ImGui.SameLine();
                    ImGui.RadioButton("WASD", ref CameraMode, 1);
                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.TextColored(Color.gray, "Draw Mode:");
                    ImGui.RadioButton("Standard", ref DebugDrawMode, 0); ImGui.SameLine();
                    ImGui.RadioButton("Debug", ref DebugDrawMode, 1);
                    ImGui.Dummy(Vector2.one * 5.0f);
                    
                    ImGui.TextColored(Color.gray, "Brush Size:");
                    ImGui.SliderFloat("##1", ref BrushSize, 0.005f, 0.25f);
                    ImGui.TextColored(Color.gray, "Brush Height:");
                    ImGui.SliderFloat("##2", ref BrushHeight, -1.0f, 1.0f);
                    ImGui.TextColored(Color.gray, "Gravity:");
                    ImGui.SliderFloat("##3", ref Gravity, -2.0f, 2.0f);

                    ImGui.Dummy(Vector2.one * 5.0f);
                
                    ImGui.TextColored(Color.gray, $"SDF Resolution ({SdfResolution * 8}^3)");
                    ImGui.SliderInt("##4", ref SdfResolution, 2, 64);
                    Reset = ImGui.Button("Reset");
                    ImGui.Dummy(Vector2.one * 5.0f);
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Rendering"))
                {
                    ImGui.TextColored(Color.gray, "AO Samples:");
                    ImGui.SliderInt("##1", ref AOSamples, 0, 32);
                    
                    ImGui.TextColored(Color.gray, "AO Kernel Size:");
                    ImGui.SliderFloat("##2", ref AOKernelSize, 0.0f, 0.25f);
                    
                    ImGui.TextColored(Color.gray, "Shadow Intensity:");
                    ImGui.SliderFloat("##5", ref ShadowIntensity, 0.0f, 1.0f);
                    
                    ImGui.TextColored(Color.gray, "Shadow Softness:");
                    ImGui.SliderFloat("##6", ref ShadowSoftness, 0.0f, 1.0f);

                    ImGui.TextColored(Color.gray, "Max Raymarch Steps:");
                    ImGui.SliderInt("##3", ref MaxRaymarchSteps, 8, 512);

                    ImGui.TextColored(Color.gray, "Sun Position:");
                    ImGui.SliderFloat3("##4", ref SunPos, -10.0f, 10.0f);
                    
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.End();
            }
        }
    }
}