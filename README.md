# 黑神话悟空CSharp加载器

[English](README.en.md)

## 关于

黑神话悟空使用USharp作为脚本引擎，很多逻辑使用C#实现，提供接口给UE使用(见GameDll)，而一些内部接口不能通过UE c++/lua访问。  
这个mod加载器可以加载c#写的mod。C# mod可以调用USharp端提供的api，包括游戏内部的和USharp包装的UE引擎的api，也可以往游戏内部的事件和委托增加回调。  

## 使用说明

### 安装路径
```
BlackMythWukong/b1/Binaries/Win64/
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

mod的dll需要放在`CSharpLoader/Mods/<ModName>/<ModName>.dll`里面，例如`CSharpLoader/Mods/CSharpExample/CSharpExample.dll`

### 配置

配置文件CSharpLoader/b1cs.ini
Develop: 开启开发模式，可以按ctrl+f5重新加载C# mods
Console: 显示控制台窗口，打印log
EnableJit: 开启jit模式，请看下面的注意

### 注意

0.0.7版本C# loader为了开启C# patch/hook特性，把游戏的mono运行时的运行模式从默认的解释执行模式，改成了jit(运行时编译执行)模式。jit一定程度上可能会带来一些性能提升，但可能并未经过游戏开发商的充分测试，可能会对游戏的稳定性带来一些影响。同时由于修改了游戏的底层执行代码，目前与一些修改器不兼容，包括风灵月影在内的一些修改器会失效，请根据你使用的mod的说明，决定是否使用支持hook的C# loader。

### 与其他插件的兼容

version.dll是常用的插件名字，如果你使用了其他有名称冲突的插件，可以把这些插件的dll，改成除version.dll外的其他名字（后缀得是.dll），放到CSharpLoader/Plugins/目录里，C# loader会一起加载这些插件

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

建议安装.net 8 sdk，方便使用最新的语法。
开发工具可以使用vscode, visual studio 或者 rider。
示例工程可以参考CSharpModExample

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

Mod加载时会创建该类的实例，调用Init。Manager重新加载mod时会调用每个mod的DeInit()，再执行加载mod的流程。  
Ctrl+F5重新加载Mod，Develop模式下，每次都会加载成新的Assembly，旧Asembly还会保留在进程中，所以请确保DeInit函数里结束mod的后台线程（如有）和事件监听，mod本身不用清理按键监听，Manager处理好。

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

### mod的其他依赖
mod dll的依赖可以放在`CSharpLoader/Mods/<ModName>/`或者`CSharpLoader/Mods/Common/`中，这样其他Mod也能用里面的依赖
