using AopStdLib;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var callable = DynamicProxy.CreateProxyOfRealize<ICallable, Person>();
            callable.Call();
            Console.Read();
        }
    }

    public interface ICallable
    {
        void Call();
    }

    [Interceptor]
    public class Person : ICallable
    {
        [Logger]
        public void Call()
        {
            Console.WriteLine("Calling...");
        }
    }

    public class InterceptorAttribute: InterceptorBaseAttribute
    {
        public override object Invoke(object target, string method, object[] parameters)
        {
            return base.Invoke(target, method, parameters);
        }
    }

    public class LoggerAttribute: ActionBaseAttribute
    {
        public override void OnBefore(string method, object[] parameters)
        {
            Console.WriteLine("On Before");
        }

        public override object OnAfter(string method, object result)
        {
            Console.WriteLine("On After");
            return result;
        }
    }
}
