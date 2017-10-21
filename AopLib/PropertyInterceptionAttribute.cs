using System;

namespace AopLib
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PropertyInterceptionAttribute : Attribute, IMethodInterceptor
    {
        public int Order { get; set; }
        public PropertyInterceptionType InterceptionType { get; set; }

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

    [Flags]
    public enum PropertyInterceptionType
    {
        None = 0,
        Get = 1,
        Set = 2
    }
}
