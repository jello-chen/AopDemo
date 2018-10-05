using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            //var proxy = new ProxyClass();
            //Console.WriteLine(proxy.GetName(2));

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 1000000; i++)
            {
                // 122ms
                //var service = new ProductService();
                //service.GetName(1);

                // 700ms
                //var service = new ProxyClass();
                //service.GetName(1);

                // 788ms
                var proxy = ProxyGenerator<ProductService>.Create();
                proxy.GetName(2);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            Console.ReadKey();
        }
    }

    public class Interceptor
    {
        public object Invoke(object @object, string @method, object[] parameters)
        {
            //Console.WriteLine("Before");
            var result = @object.GetType().GetMethod(method).Invoke(@object, parameters);
            //Console.WriteLine("After");
            return result;
        }
    }

    public class ProductService
    {
        public virtual string GetName(int id)
        {
            return id.ToString();
        }
    }

    public class ProxyClass : ProductService
    {
        private Interceptor interceptor = new Interceptor();

        public override string GetName(int id)
        {
            var service = new ProductService();
            return interceptor.Invoke(service, nameof(GetName), new object[] { id }).ToString();
        }
    }

    class ProxyGenerator<T>
    {
        private static IDictionary<Type, Type> proxyCache = new Dictionary<Type, Type>();

        public static T Create()
        {
            var type = typeof(T);

            if(!proxyCache.TryGetValue(type, out Type proxyType))
            {
                var assemblyName = type.Name + "_Assembly";
                var moduleName = type.Name + "_Module";
                var typeName = type.Name + "_Type";

                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
                var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, type);

                // Define interceptor field
                var interceptorField = typeBuilder.DefineField("_interceptor", typeof(Interceptor), FieldAttributes.Private);
                var typeConstructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);

                // Assign value for interceptor
                var ilTypeConstructor = typeConstructor.GetILGenerator();
                ilTypeConstructor.Emit(OpCodes.Ldarg_0);
                ilTypeConstructor.Emit(OpCodes.Newobj, typeof(Interceptor).GetConstructor(Type.EmptyTypes));
                ilTypeConstructor.Emit(OpCodes.Stfld, interceptorField);
                ilTypeConstructor.Emit(OpCodes.Ret);

                var ignoredMethodNames = typeof(object).GetMethods().Select(m => m.Name).ToArray();
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).ToArray();
                foreach (var method in methods)
                {
                    var methodName = method.Name;

                    if (ignoredMethodNames.Contains(methodName)) continue;

                    var parameters = method.GetParameters();
                    var methodBuilder = typeBuilder.DefineMethod(
                        methodName,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        method.ReturnType,
                        method.GetParameters().Select(p => p.ParameterType).ToArray());
                    var ilMethod = methodBuilder.GetILGenerator();

                    // Define some local variables
                    var targetBuilder = ilMethod.DeclareLocal(type);
                    var parameterBuilder = ilMethod.DeclareLocal(typeof(object[]));

                    // Construct an instance Of T
                    ilMethod.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                    ilMethod.Emit(OpCodes.Stloc, targetBuilder);

                    // Construct parameter of object[]
                    ilMethod.Emit(OpCodes.Ldc_I4, parameters.Length);
                    ilMethod.Emit(OpCodes.Newarr, typeof(object));
                    ilMethod.Emit(OpCodes.Stloc, parameterBuilder);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ilMethod.Emit(OpCodes.Ldloc, parameterBuilder);
                        ilMethod.Emit(OpCodes.Ldc_I4, i);
                        ilMethod.Emit(OpCodes.Ldarg, i + 1);
                        ilMethod.Emit(OpCodes.Box, parameters[i].ParameterType);
                        ilMethod.Emit(OpCodes.Stelem_Ref);
                    }

                    // Call `Invoke` of Interceptor
                    ilMethod.Emit(OpCodes.Ldarg_0);
                    ilMethod.Emit(OpCodes.Ldfld, interceptorField);
                    ilMethod.Emit(OpCodes.Ldloc, targetBuilder);
                    ilMethod.Emit(OpCodes.Ldstr, methodName);
                    ilMethod.Emit(OpCodes.Ldloc, parameterBuilder);
                    ilMethod.Emit(OpCodes.Call, typeof(Interceptor).GetMethod("Invoke"));

                    // Handle return type
                    if (method.ReturnType == typeof(void))
                    {
                        ilMethod.Emit(OpCodes.Pop);
                    }
                    else
                    {
                        if (method.ReturnType.IsValueType)
                        {
                            ilMethod.Emit(OpCodes.Unbox_Any, method.ReturnType);
                        }
                        else
                        {
                            ilMethod.Emit(OpCodes.Castclass, method.ReturnType);
                        }
                    }

                    ilMethod.Emit(OpCodes.Ret);
                }
                proxyType = typeBuilder.CreateTypeInfo();
                proxyCache.Add(type, proxyType);
            }
            
            return (T)Activator.CreateInstance(proxyType);
        }
    }
}
