using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AopLib
{
    public class Class1
    {
        public async void Test(int i)
        {
            Console.WriteLine("1");
            await Task.Delay(1000);
            Console.WriteLine("2");
            await Task.Delay(2000);
        }
    }
}
