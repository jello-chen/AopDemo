using System;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //try
            //{
            //    Console.WriteLine(new ClassLibrary1.Test().TestReturnMethod(5));
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("execption:" + ex.Message);
            //}

            //Console.WriteLine(new ClassLibrary1.Test().TestGetProperty);

            //new ClassLibrary1.Test().TestSetProperty = 2;

            //new ClassLibrary1.Test().TestNoneProperty = 1;

            var test = new ClassLibrary1.Test();
            test.TestProperty = 2;
            Console.WriteLine(test.TestProperty);

            Console.ReadKey();
        }
    }
}
