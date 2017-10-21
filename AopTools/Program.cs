using AopInterceptionTaskLib;
using System;

namespace AopTools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Inject assembly failed, argument is error.");
                return;
            }
            System.IO.File.AppendAllText("D:\\1.log", args[0] + "\r\n");
            IAopInterceptionTask interceptionTask = new AopInterceptionTask(args[0]);
            interceptionTask.Run();
            Console.ReadKey();
        }
    }
}
