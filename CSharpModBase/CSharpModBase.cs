namespace CSharpModBase
{
    public static class Common
    {
        public const string LoaderDir = "CSharpLoader";
        public const string ModDir = "CSharpLoader\\Mods";
        public const string DataDir = "CSharpLoader\\Data";
    }


    public interface ICSharpMod
    {
        /// <summary>
        /// mod name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// mod version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// when mod loaded, will call OnInit
        /// </summary>
        void Init();
        /// <summary>
        /// when manamger reload mods, will call OnDeInit,
        ///
        /// </summary>
        void DeInit();
    }
}
