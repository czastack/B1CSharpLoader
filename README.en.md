# CSharp mod loader for BlackMythWukong

[中文](README.md)

## About

BlackMythWukong use USharp as script engine, many logic implements in charp (see GameDll).  
This mod loader allow you to load mods written in C#.  
C# mods can call csharp api from b1-managed and Unreal Engine.  

## How to use

### Install path
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

Mod dll should be placed in `CSharpLoader/Mods/<ModName>/<ModName>.dll`, for example `CSharpLoader/Mods/CSharpExample/CSharpExample.dll`

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

### C# hook (Patch)

not implemented yet

### Mod deps
Mod depends dll can put in `CSharpLoader/Mods/<ModName>/` or `CSharpLoader/Mods/Common/`, Common is shared by all mods.
