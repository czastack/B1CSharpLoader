using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;

namespace CSharpExample
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";
        // private readonly Harmony harmony;

        public MyMod()
        {
            // harmony = new Harmony(Name);
        }

        public void Init()
        {
            Console.WriteLine($"{Name} Init");
            Utils.RegisterKeyBind(Key.ENTER, () => Console.WriteLine("Enter pressed"));
            Utils.RegisterKeyBind(ModifierKeys.Control, Key.ENTER, () => Console.WriteLine("Ctrl+Enter pressed"));

            // hook
            // harmony.PatchAll();
        }

        public void DeInit()
        {
            Console.WriteLine($"{Name} DeInit");
            // harmony.UnpatchAll();
        }
    }
}
