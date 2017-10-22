using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AopLib.RT
{
    public static class DynamicProxyGenerator
    {
        public static T GetInstanceFor<T>()
        {
            Type typeOfT = typeof(T);
            var methodInfos = typeOfT.GetMethods();
            AssemblyName assName = new AssemblyName("testAssembly");
            var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assBuilder.DefineDynamicModule("testModule", "test.dll");
            var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "Proxy", TypeAttributes.Public);

            if(typeOfT.IsGenericType)
            {
                GenericsHelper.MakeGenericType(typeOfT, typeBuilder);
            }

            typeBuilder.AddInterfaceImplementation(typeOfT);
            var ctorBuilder = typeBuilder.DefineConstructor(
                      MethodAttributes.Public,
                      CallingConventions.Standard,
                      new Type[] { });
            var ilGenerator = ctorBuilder.GetILGenerator();
            ilGenerator.EmitWriteLine("Creating Proxy instance");
            ilGenerator.Emit(OpCodes.Ret);
            foreach (var methodInfo in methodInfos)
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    methodInfo.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    methodInfo.ReturnType,
                    methodInfo.GetParameters().Select(p => p.GetType()).ToArray()
                    );
                var methodILGen = methodBuilder.GetILGenerator();
                if (methodInfo.ReturnType == typeof(void))
                {
                    methodILGen.Emit(OpCodes.Ret);
                }
                else
                {
                    if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum)
                    {
                        MethodInfo getMethod = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type)});
                        LocalBuilder lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
                        methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
                        methodILGen.Emit(OpCodes.Call, (typeof(Type)).GetMethod("GetTypeFromHandle"));
                        methodILGen.Emit(OpCodes.Callvirt, getMethod);
                        methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);

                    }
                    else
                    {
                        methodILGen.Emit(OpCodes.Ldnull);
                    }
                    methodILGen.Emit(OpCodes.Ret);
                }
                typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            }

            Type constructedType = typeBuilder.CreateType();
            var instance = Activator.CreateInstance(constructedType);
            return (T)instance;
        }
    }

    /// <summary>
    /// Helper class for generic types and methods.
    /// </summary>
    internal static class GenericsHelper
    {
        /// <summary>
        /// Makes the typeBuilder a generic.
        /// </summary>
        /// <param name="concrete">The concrete.</param>
        /// <param name="typeBuilder">The type builder.</param>
        public static void MakeGenericType(Type baseType, TypeBuilder typeBuilder)
        {
            Type[] genericArguments = baseType.GetGenericArguments();
            string[] genericArgumentNames = GetArgumentNames(genericArguments);
            GenericTypeParameterBuilder[] genericTypeParameterBuilder
                = typeBuilder.DefineGenericParameters(genericArgumentNames);
            typeBuilder.MakeGenericType(genericTypeParameterBuilder);
        }
        /// <summary>
        /// Gets the argument names from an array of generic argument types.
        /// </summary>
        /// <param name="genericArguments">The generic arguments.</param>
        public static string[] GetArgumentNames(Type[] genericArguments)
        {
            string[] genericArgumentNames = new string[genericArguments.Length];
            for (int i = 0; i < genericArguments.Length; i++)
            {
                genericArgumentNames[i] = genericArguments[i].Name;
            }
            return genericArgumentNames;
        }
    }
}
