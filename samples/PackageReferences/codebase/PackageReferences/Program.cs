using System;
using Newtonsoft.Json;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            string json = JsonConvert.SerializeObject("Hello World", Formatting.Indented);
            Console.Write(json);
        }
    }
}
