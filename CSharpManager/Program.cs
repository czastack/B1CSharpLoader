
namespace CSharpManager
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CSharpModManager.Instance.LoadMods();
            CSharpModManager.Instance.StartLoop();
        }
    }
}
