using System;

namespace CSharpModBase
{
    public static class CSharpLoader
    {
        private static ICSharpModManager CSharpModManager => CSharpLoaderInternal.CSharpModManager!;

        /// <summary>
        /// Version of CSharpLoader
        /// </summary>
        public static Version Version => CSharpModManager.Version;

        /// <summary>
        /// ImGuiOverlay is drawing UI
        /// </summary>
        public static bool IsDrawingUI => CSharpModManager.IsDrawingUI;

        /// <summary>
        /// ImGuiOverlay is drawing mods UI
        /// </summary>
        public static bool IsDrawingModsUI => CSharpModManager.IsDrawingModsUI;

        /// <summary>
        /// Window DPI scale
        /// </summary>
        public static float DpiScale => CSharpModManager.DpiScale;
    }
}
