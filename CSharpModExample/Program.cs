using System;
using System.Numerics;
using b1;
using BtlShare;
using CSharpModBase;
using CSharpModBase.Input;
using ImGuiNET;
// using HarmonyLib;

namespace CSharpExample
{
    public class MyMod : ICSharpMod, IGuiMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";
        private bool showWindow = true;
        private bool drawBackground = true;
        // private readonly Harmony harmony;

        public MyMod()
        {
            // harmony = new Harmony(Name);
            // Harmony.DEBUG = true;
        }

        public void Init()
        {
            Console.WriteLine($"{Name} Init");
            Utils.RegisterKeyBind(Key.ENTER, () => Console.WriteLine("Enter pressed"));
            Utils.RegisterKeyBind(ModifierKeys.Control, Key.ENTER, FindPlayer);

            // hook
            // harmony.PatchAll();
        }

        public void DeInit()
        {
            Console.WriteLine($"{Name} DeInit");
            // harmony.UnpatchAll();
        }

        private void FindPlayer()
        {
            Console.WriteLine("Ctrl+Enter pressed");
            var player = MyUtils.GetControlledPawn();
            if (player == null)
            {
                Console.WriteLine("Player not found");
            }
            else
            {
                Console.WriteLine($"Player found: {player}");
                float hp = BGUFunctionLibraryCS.GetAttrValue(player, EBGUAttrFloat.Hp);
                float hpMax = BGUFunctionLibraryCS.GetAttrValue(player, EBGUAttrFloat.HpMax);
                Console.WriteLine($"HP: {hp}/{hpMax}");
            }
        }

        public void Render()
        {
            if (CSharpLoader.IsDrawingModsUI)
            {
                if (ImGui.TreeNode("MyMod"))
                {
                    ImGui.Checkbox("Show Window", ref showWindow);
                    ImGui.Checkbox("Show Watermark", ref drawBackground);
                    ImGui.TreePop();
                }
            }
            if (showWindow && CSharpLoader.IsDrawingUI)
            {
                ImGui.Begin("MyMod Window", ref showWindow);
                ImGui.Text("Hello from MyMod!");
                if (ImGui.Button("Close Me"))
                    showWindow = false;
                ImGui.End();
            }
            // Draw background
            if (drawBackground)
            {
                var io = ImGui.GetIO();
                var pos = new Vector2(io.DisplaySize.X / 2, 100);
                // Use APlayerController.ProjectWorldLocationToScreen to get screen pos
                ImGui.GetBackgroundDrawList().AddText(null, 20 * CSharpLoader.DpiScale, pos, 0xFFFFFFFF, "Welcome to MyMod");
            }
        }
    }
}
