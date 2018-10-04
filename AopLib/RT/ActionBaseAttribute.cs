using System;

namespace AopLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ActionBaseAttribute: Attribute
    {
        public virtual bool OnBefore(string method, object[] parameters) => true;
        public virtual object OnAfter(string method, object result) => result;
        public virtual ExceptionStrategy OnException(Exception e) => ExceptionStrategy.ReThrow;
    }
}
