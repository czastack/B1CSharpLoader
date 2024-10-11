# CSharp mod loader for BlackMythWukong

[中文](README.md)

## About

BlackMythWukong use USharp as script engine, many logic implements in C# (see GameDll). Some api can only access in C#, not in UE c++.  
This mod loader allow you to load mods written in C#. C# mods can call C# api from b1-managed and Unreal Engine.  

## How to use

### Install path
```
b1/Binaries/Win64/
  CSharpLoader/
    Data/
    Mods/
    Plugins/
    0Harmony.dll
    CSharpManager.bin
    CSharpModBase.dll
    Mono.Cecil.dll
    Mono.Cecil.Pdb.dll
    Mono.Cecil.Rocks.dll
    b1cs.ini
  version.dll
```

Mod dll should be placed in `CSharpLoader/Mods/<ModName>/<ModName>.dll`, for example `CSharpLoader/Mods/CSharpExample/CSharpExample.dll`

### config file
CSharpLoader/b1cs.ini
Develop: press ctrl+f5 reload C# mods
Console: show console window to print log
EnableJit: see Attention below

### Attention

In version 0.0.7 of the C# loader, to enable C# patch/hook features, the game's Mono runtime execution mode has been changed from the default interpreter mode to the JIT (Just-In-Time) mode. JIT can potentially bring some performance improvements, but it may not have been thoroughly tested by the game developers, which could impact the game's stability. Additionally, it is currently incompatible with some trainer, including flingtrainer, which may become ineffective. Please decide whether to use the C# loader that supports hooks based on the instructions of the mods you are using.

### Compatibility with Other Plugins

"version.dll" is a commonly used plugin name. If you are using other plugins with naming conflicts, you can rename their DLLs to any name other than "version.dll" (the suffix must be ".dll") and place them in the "CSharpLoader/Plugins/" directory. The C# loader will load these plugins together.

## modules

- CSharpLoaderDll
  - loader for CSharpManager
- CSharpManager
  - manager of C# mod
- CSharpModBase
  - interface for mods
- CSharpModExample
  - mod example
- GameDll
  - inner C# dll of game

## Mod develop tutorial

Suggest install [.net 8 sdk](https://dotnet.microsoft.com/) to develop with latest syntax.
You can use vscode, visual studio or rider to write code.
See CSharpModExample.

### Mod entrance

Your mod should implements ICSharpMod

```C#
public class MyMod : ICSharpMod
{
    public string Name => "ModExample";
    public string Version => "0.0.1";

    public void Init()
    {
        Console.WriteLine("Init");
    }

    public void DeInit()
    {
        Console.WriteLine("DeInit");
    }
}
```

Manager will create instance of class that implements ICSharpMod and call Init.  
When reload mods by Ctrl+F5, Manager will call DeInit() of loaded mods, then reload all mods.  
In Develop mode, mod dll will load as new Assembly, why old Assembly keep in memory. So make sure finish your background thread and clear event handler in DeInit function..
Mod do not need to clear key listening, manager will do it.


### KeyBind

```C#
Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
{

});
```


### C# hook
see [harmony document](https://harmony.pardeike.net/articles/patching.html)

call in Init()
```C#
var harmony = new Harmony("your name");
var assembly = Assembly.GetExecutingAssembly();
harmony.PatchAll(assembly);

// or implying current assembly:
harmony.PatchAll();
```

call in DeInit()
```C#
harmony.UnpatchAll();
```

### Mod deps
Mod depends dll can put in `CSharpLoader/Mods/<ModName>/` or `CSharpLoader/Mods/Common/`, Common is shared by all mods.
