# CSharp mod loader for BlackMythWukong

[中文](README.md)

## About

BlackMythWukong use USharp as script engine, many logic implements in charp (see GameDll).  
This mod loader allow you to load mods written in C#.  
C# mods can call csharp api from b1-managed and Unreal Engine.  

## How to use

install path
```
b1/Binaries/Win64/
  CSharpLoader/
    Data/
    Mods/
    0Harmony.dll
    CSharpManager.bin
    CSharpModBase.dll
    Mono.Cecil.dll
    Mono.Cecil.Pdb.dll
    Mono.Cecil.Rocks.dll
    b1cs.ini
  hid.dll
```

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

## mod tutorial

see CSharpModExample

### mod entrance

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
When reload mods by Ctrl+Shift+R, Manager will call DeInit() of loaded mods, then reload all mods.  
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

### mod deps
mod depends dll can put in `CSharpLoader/Mods/<mod file name>/` or `CSharpLoader/Mods/Common/`, Common is shared by all mods.