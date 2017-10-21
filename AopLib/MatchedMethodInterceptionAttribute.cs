using System;

namespace AopLib
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MatchedMethodInterceptionAttribute : Attribute, IMethodInterceptor
    {
        public int Order { get; set; }

        public string Rule { get; set; }

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
