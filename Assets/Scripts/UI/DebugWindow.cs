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

namespace UImGui
{
    public class DebugWindow : MonoBehaviour
    {
        private int _mode = 0;
        private float _size = 0.05f;
        private bool _reset = false;
        private bool _spawn = false;
        private float _gravity = 1.0f;

        public int Mode => _mode;
        public float Size => _size;
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

        private void OnEnable()
        {
            UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGuiUtility.Layout -= OnLayout;
        }

        private void OnLayout(UImGui uImGui)
        {
            if (ImGui.Begin("Options"))
            {
                ImGui.SetNextWindowSize(Vector2.one * 200, ImGuiCond.Once);

                ImGui.RadioButton("Add", ref _mode, 0); 
                ImGui.SameLine();
                ImGui.RadioButton("Cut", ref _mode, 1);
                
                ImGui.Dummy(Vector2.one * 20.0f);

                ImGui.SliderFloat("Size", ref _size, 0.005f, 0.25f);
                ImGui.SliderFloat("Gravity", ref _gravity, -2.0f, 2.0f);
                
                ImGui.Dummy(Vector2.one * 20.0f);

                _reset = ImGui.Button("Reset");
                _spawn = ImGui.Button("Spawn");
                
                ImGui.End();
            }
        }
    }
}