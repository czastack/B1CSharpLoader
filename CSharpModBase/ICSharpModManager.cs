using System;
using CSharpModBase.Input;

namespace CSharpModBase
{
    public interface ICSharpModManager
    {
        Version Version { get; }
        bool IsDrawingUI { get; }
        bool IsDrawingModsUI { get; }
        float DpiScale { get; }
    }
}
