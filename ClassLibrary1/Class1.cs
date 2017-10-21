using System;
using AopLib;

namespace ClassLibrary1
{
    [MatchedMethodAround(Rule = "*Matched*")]
    public class Test
    {
        [MethodAround]
        public void TestVoidMethod(int i)
        {
            Console.WriteLine("Execute TestVoidMethod: " + i);
        }

        [MethodAround]
        public int TestReturnMethod(int i)
        {
            Console.WriteLine("Execute TestReturnMethod: " + i.ToString());
            return i / 0;
        }

        public void TestMatchedMethod()
        {
            Console.WriteLine("Execute TestMatchedMethod");
        }

        [ProperyAround(InterceptionType = PropertyInterceptionType.Get)]
        public int TestGetProperty { get; set; }

        [ProperyAround(InterceptionType = PropertyInterceptionType.Set)]
        public int TestSetProperty { get; set; }

        [ProperyAround(InterceptionType = PropertyInterceptionType.None)]
        public int TestNoneProperty { get; set; }

        [ProperyAround(InterceptionType = PropertyInterceptionType.Get | PropertyInterceptionType.Set)]
        public int TestProperty { get; set; }
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

    public class MatchedMethodAroundAttribute : MatchedMethodInterceptionAttribute
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

    public class ProperyAroundAttribute : PropertyInterceptionAttribute
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
