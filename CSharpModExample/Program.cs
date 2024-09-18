
using CSharpModBase;

namespace CSharpExample
{
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
}
