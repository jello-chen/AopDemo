using System;

namespace AopLib
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class InterceptorBaseAttribute : Attribute
    {
        public virtual object Invoke(object target, string method, object[] parameters)
        {
            return target.GetType().GetMethod(method).Invoke(target, parameters);
        }
    }
}
