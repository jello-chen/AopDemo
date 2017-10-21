using System;
using AopLib;

namespace ClassLibrary1
{
    public class Test
    {
        [MethodAround]
        public int TestMethod(int i)
        {
            Console.WriteLine("Execute: " + i);
            return i + 1;
        }
    }

    public class MethodAroundAttribute : MethodInterceptionAttribute
    {
        public override bool BeforeExecute(MethodExecutionEventArgs args)
        {
            Console.WriteLine("------------------");
            Console.WriteLine(args.Instance);
            Console.WriteLine(args.Method);
            Console.WriteLine(this.GetType() + ":" + "before execute");
            return true;
        }

        public override ExceptionStrategy OnExecption(MethodExecutionEventArgs args)
        {
            Console.WriteLine("------------------");
            Console.WriteLine("exception:" + args.Exception);
            return ExceptionStrategy.Handle;
        }

        public override void AfterExecute(MethodExecutionEventArgs args)
        {
            Console.WriteLine("------------------");
            Console.WriteLine(this.GetType() + ":" + "after execute" + ", result:" + args.ReturnValue);
        }
    }
}
