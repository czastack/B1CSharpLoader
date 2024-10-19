namespace CSharpModBase;

public interface ICSharpMod
{
    /// <summary>
    ///     mod name
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     mod version
    /// </summary>
    string Version { get; }

    /// <summary>
    ///     when mod loaded, will call OnInit
    /// </summary>
    void Init();

    /// <summary>
    ///     when manager reload mods, will call OnDeInit,
    /// </summary>
    void DeInit();
}