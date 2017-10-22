using System;

namespace AopLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class MethodInterceptionAttribute : Attribute, IMethodInterceptor
    {
        public int Order { get; set; }

        public virtual void AfterExecute(MethodExecutionEventArgs args)
        {
            
        }

        public virtual bool BeforeExecute(MethodExecutionEventArgs args)
        {
            return true;
        }

        public virtual ExceptionStrategy OnExecption(MethodExecutionEventArgs args)
        {
            return ExceptionStrategy.ReThrow;
        }
    }
}
