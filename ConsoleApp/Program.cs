using AopLib;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

            //var test = new ClassLibrary1.Test();
            //test.TestProperty = 2;
            //Console.WriteLine(test.TestProperty);

            //var callable = DynamicProxy.CreateProxyOfRealize<ICalculator, Person>();
            //Console.WriteLine(callable.Calculate(1, 2));

            T2().Wait();

            Console.ReadKey();
        }

        static async Task T1()
        {
            if (await Before())
            {
                Console.WriteLine("JELLO");
            }
            await After();
        }

        static async Task T2()
        {
            Console.WriteLine("11");
            await After();
            Console.WriteLine("OK");
        }

        static bool GetBool()
        {
            return Environment.TickCount % 2 == 0;
        }

        static Task<bool> Before()
        {
            Console.WriteLine("Before");
            return Task.FromResult(true);
        }

        static Task After()
        {
            Console.WriteLine("After");
            return Task.FromResult(0);
        }

        #region AsyncStateMachine
        static Task T11()
        {
            MyStateMachine myStateMachine = new MyStateMachine();
            myStateMachine.asyncTaskMethodBuilder = AsyncTaskMethodBuilder.Create();
            myStateMachine.state = -1;
            myStateMachine.asyncTaskMethodBuilder.Start<MyStateMachine>(ref myStateMachine);
            return myStateMachine.asyncTaskMethodBuilder.Task;
        }

        class MyStateMachine : IAsyncStateMachine
        {
            public int state;
            public AsyncTaskMethodBuilder asyncTaskMethodBuilder;
            private bool s1;
            private TaskAwaiter<bool> u1;
            private TaskAwaiter u2;

            public void MoveNext()
            {
                int num = state;
                TaskAwaiter<bool> awaiter2;
                TaskAwaiter awaiter;
                MyStateMachine stateMachine;

                try
                {
                    if (num == 0) goto IL_0012;
                    else
                    {
                        if (num == 1) goto IL_0014;
                        else
                        {
                            awaiter2 = Before().GetAwaiter();
                            if (!awaiter2.IsCompleted)
                            {
                                num = state = 0;
                                u1 = awaiter2;
                                stateMachine = this;
                                asyncTaskMethodBuilder.AwaitUnsafeOnCompleted(ref awaiter2, ref stateMachine);
                            }
                            else
                            {
                                goto IL_0071;
                            }
                        }
                    }
                    state = -2;
                    asyncTaskMethodBuilder.SetResult();
                }
                catch (Exception ex)
                {
                    state = -2;
                    asyncTaskMethodBuilder.SetException(ex);
                }


                IL_0012:
                {
                    awaiter2 = u1;
                    u1 = default(TaskAwaiter<bool>);
                    num = state = -1;
                }
                IL_0014:
                {
                    awaiter = u2;
                    u2 = default(TaskAwaiter);
                    num = state = -1;
                }
                IL_0071:
                {
                    s1 = awaiter2.GetResult();
                    if (!s1) goto IL_0095;
                    Console.WriteLine("JELLO");
                }
                IL_0095:
                {
                    awaiter = After().GetAwaiter();
                    if (!awaiter.IsCompleted)
                    {
                        num = state = -1;
                        u2 = awaiter;
                        stateMachine = this;
                        asyncTaskMethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
                    }
                    else
                    {
                        awaiter.GetResult();
                    }
                }
            }

            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {

            }
        } 
        #endregion
    }

    public interface ICalculator
    {
        int Calculate(int i, int j);
    }

    [Interceptor]
    public class Person : ICalculator
    {
        [Logger]
        public int Calculate(int i, int j)
        {
            Console.WriteLine(i + j);
            return i + j;
        }
    }

    public class InterceptorAttribute : InterceptorBaseAttribute
    {
        public override object Invoke(object target, string method, object[] parameters)
        {
            return base.Invoke(target, method, parameters);
        }
    }

    public class LoggerAttribute : ActionBaseAttribute
    {
        public override bool OnBefore(string method, object[] parameters)
        {
            Console.WriteLine("On Before");
            return true;
        }

        public override object OnAfter(string method, object result)
        {
            Console.WriteLine("On After:" + result);
            return result;
        }
    }

    struct MyStruct
    {

    }
}
