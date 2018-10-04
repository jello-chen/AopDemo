using System;

namespace AopStdLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ActionBaseAttribute: Attribute
    {
        public virtual void OnBefore(string method, object[] parameters) { }
        public virtual object OnAfter(string method, object result) => result;
        public virtual ExceptionStrategy OnException(Exception e) => ExceptionStrategy.ReThrow;
    }
    public enum ExceptionStrategy
    {
        Handle,
        ReThrow,
        ThrowNew
    }
}
