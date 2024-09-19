# 黑神话悟空CSharp加载器

[English](README.en.md)

## 关于

黑神话悟空使用USharp作为脚本引擎，很多逻辑使用charp实现，提供接口给UE使用(见GameDll)。  
这个mod加载器可以加载c#写的mod。C# mod可以调用USharp端提供的api，包括游戏内部的和USharp包装的UE引擎的api，也可以往游戏内部的事件和委托增加回调。  

## 使用说明

安装路径
```
b1/
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

## 模块介绍

- CSharpLoaderDll
  - 加载器，用于调用mono api执行C#代码，加载器会在游戏启动后调用CSharpManager
- CSharpManager
  - C# mod管理器，加载和管理C# mod
- CSharpModBase
  - 提供给mod使用的接口
- CSharpModExample
  - mod示例
- GameDll
  - 游戏内的C# dll，写mod代码需要引用这些dll

## mod编写教程

参考CSharpModExample

### mod入口

mod的类需要实现ICSharpMod接口

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

Mod加载时会创建该类的实例，调用Init，重新加载时会调用DeInit()，再重新创建新实例。Ctrl+Shift+R重新加载Mod，Develop模式下，每次都会加载新的Assembly，静态变量都会重置。旧Asembly还会保留在进程中，所以请确保DeInit函数里结束mod的后台线程（如有）和事件监听。

### 按键监听

```C#
Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
{

});
```

### C# hook

参考 [harmony文档](https://harmony.pardeike.net/articles/patching.html)

Init()里调用

```C#
var harmony = new Harmony("myname");
var assembly = Assembly.GetExecutingAssembly();
harmony.PatchAll(assembly);

// or implying current assembly:
harmony.PatchAll();
```

DeInit()里调用

```C#
harmony.UnpatchAll();
```
