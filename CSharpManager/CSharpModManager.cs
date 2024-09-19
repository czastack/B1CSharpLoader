using System.Reflection;
using CSharpModBase;
using CSharpModBase.Input;
using Mono.Cecil;

namespace CSharpManager
{
    public class CSharpModManager
    {
        static string? LoadingModName { get; set; }

        public List<ICSharpMod> LoadedMods { get; } = new();
        public InputManager InputManager { get; } = new();
        public bool Develop { get; set; }
        private Thread? loopThread;

        static CSharpModManager()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += AssemblyResolve;
            currentDomain.UnhandledException += OnUnhandledException;
        }

        private static Assembly? AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (LoadingModName == null)
                {
                    return null;
                }
                string modName = LoadingModName;
                string assemblyPath = Path.Combine(Common.ModDir, modName, $"{args.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
                string assemblyPathCommon = Path.Combine(Common.ModDir, "Common", $"{args.Name}.dll");
                if (File.Exists(assemblyPathCommon))
                {
                    return Assembly.LoadFrom(assemblyPathCommon);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Load assembly {args.Name} failed:");
                Log.Error(ex);
            }
            return Assembly.LoadFrom(args.Name);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error($"UnhandledException: {(Exception)e.ExceptionObject}");
        }

        public CSharpModManager()
        {
            Utils.InitInputManager(InputManager);
            Develop = true;
            // TODO: load config from ini
        }

        public void LoadMods()
        {
            LoadedMods.Clear();
            if (!Directory.Exists(Common.ModDir))
            {
                Log.Error($"Mod dir {Common.ModDir} not exists");
                return;
            }
            string[] files = Directory.GetFiles(Common.ModDir, "*.dll");
            Type ICSharpModType = typeof(ICSharpMod);
            foreach (var dllPath in files)
            {
                try
                {
                    Log.Debug($"Load mod from {dllPath}");
                    LoadingModName = Path.GetFileNameWithoutExtension(dllPath);
                    Assembly assembly;
                    if (Develop)
                    {
                        var assemblyDef = AssemblyDefinition.ReadAssembly(dllPath);
                        assemblyDef.Name.Name += DateTime.Now.ToString("_yyyyMMdd_HHmmssffff");
                        using MemoryStream stream = new();
                        assemblyDef.Write(stream);
                        assembly = Assembly.Load(stream.GetBuffer());
                    }
                    else
                    {
                        assembly = Assembly.LoadFrom(dllPath);
                    }
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (ICSharpModType.IsAssignableFrom(type))
                        {
                            Log.Debug($"{type} is ICSharpMod");

                            if (Activator.CreateInstance(type) is ICSharpMod mod)
                            {
                                mod.Init();
                                LoadedMods.Add(mod);
                            }
                        }
                    }
                    LoadingModName = null;
                }
                catch (Exception e)
                {
                    Log.Error($"Load {dllPath} failed:");
                    Log.Error(e);
                }
            }
        }

        public void ReloadMods()
        {
            Log.Debug("ReloadMods");
            InputManager.Clear();
            foreach (var mod in LoadedMods)
            {
                try
                {
                    mod.DeInit();
                }
                catch (Exception e)
                {
                    Log.Error($"DeInit {mod.Name} failed:");
                    Log.Error(e);
                }
            }
            LoadMods();
        }

        public void StartLoop()
        {
            InputManager.RegisterBuiltinKeyBind(ModifierKeys.Control | ModifierKeys.Shift, Key.R, ReloadMods);
            loopThread = new Thread(Loop)
            {
                // IsBackground = true,
            };
            loopThread.Start();
        }

        private void Loop()
        {
            while (true)
            {
                InputManager.Update();
                Thread.Sleep(10);  // 10ms
            }
        }
    }
}
