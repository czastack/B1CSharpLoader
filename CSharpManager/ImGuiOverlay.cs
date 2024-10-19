using ClickableTransparentOverlay;
using System.Threading.Tasks;
using ImGuiNET;
using System.IO;
using CSharpModBase;
using CSharpModBase.Input;
using System;

namespace CSharpManager
{

    internal class ImGuiOverlay : Overlay
    {
        private bool wantKeepDemoWindow = false;
        private bool isDrawingUI = true;
        private bool isDrawingModsUI = true;
        public bool IsDrawingUI { get => isDrawingUI; }
        public bool IsDrawingModsUI { get => isDrawingUI && isDrawingModsUI; }

        public ImGuiOverlay() : base()
        {
        }

        protected override Task PostInitialized()
        {
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard |
                ImGuiConfigFlags.NavEnableGamepad |
                ImGuiConfigFlags.DockingEnable;
            io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;
            return base.PostInitialized();
        }

        protected override void AddFonts()
        {
            // base.AddFonts();
            ImGuiIOPtr io = ImGui.GetIO();
            const string fontPath = "c:\\Windows\\Fonts\\msyh.ttc";
            if (File.Exists(fontPath))
            {
                io.Fonts.AddFontFromFileTTF(fontPath, 18.0f * DpiScale, null, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
            }
        }

        protected override void Render()
        {
            if (NativeMethods.IsKeyPressedAndNotTimeout((int)Key.INSERT))
            {
                ToggleVisible();
            }
            IntPtr foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero ||
                foregroundWindow == Window.Handle ||
                foregroundWindow == InputManager.Instance.HWnd)
            {
                if (isDrawingUI)
                {
                    ImGui.Begin("CSharpLoader", ref isDrawingUI);
                    ImGui.Text("Press Insert to toggle window");
                    ImGui.Checkbox("Demo Window", ref wantKeepDemoWindow);
                    // float framerate = ImGui.GetIO().Framerate;
                    // ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");
                    if (wantKeepDemoWindow)
                    {
                        ImGui.ShowDemoWindow(ref wantKeepDemoWindow);
                    }
                    ImGui.SetNextItemOpen(true);
                    isDrawingModsUI = ImGui.TreeNode("Mods UI");
                }
                var mods = CSharpModManager.Instance.LoadedMods;
                lock (mods)
                {
                    foreach (var mod in mods)
                    {
                        if (mod is IGuiMod guiMod)
                        {
                            Utils.TryRun(guiMod.Render);
                        }
                    }
                }
                if (isDrawingUI)
                {
                    if (isDrawingModsUI) ImGui.TreePop();
                    ImGui.End();
                }
            }
        }

        public void ToggleVisible()
        {
            isDrawingUI = !isDrawingUI;
        }
    }
}
