using AopLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AopInterceptionTaskLib
{
    public class AopInterceptionTask : IAopInterceptionTask
    {
        private readonly string assemblyPath;
        private readonly AssemblyDefinition assemblyDefinition;

        public AopInterceptionTask(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
                throw Error.ArgumentNullOrEmpty(nameof(assemblyPath));

            this.assemblyPath = assemblyPath;
            this.assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        }

        public void Run()
        {
            System.IO.File.AppendAllText("d:\\1.log", AopLib.Error.Format("{0}", "wojf") + "\r\n");
            try
            {
                Inject();
                System.IO.File.AppendAllText("d:\\1.log", "Inject OK" + "\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("d:\\1.log", ex.Message + "\r\n");
            }
            assemblyDefinition.Write(assemblyPath);
            System.IO.File.AppendAllText("d:\\1.log", "OK" + "\r\n");
        }

        private void Inject()
        {
            var modules = GetModuleCollection();
            var types = modules.SelectMany(m => GetTypeCollection(m));
            System.IO.File.AppendAllText("d:\\1.log", types.ToList().Count.ToString()+ "\r\n");
            foreach (var type in types)
            {
                InjectType(type);
            }
        }

        protected virtual IEnumerable<ModuleDefinition> GetModuleCollection()
        {
            return assemblyDefinition.Modules;
        }

        protected virtual IEnumerable<TypeDefinition> GetTypeCollection(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Types
                .Where(t =>
                        !t.IsSpecialName &&
                        !t.CustomAttributes.Any(c => c.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName));
        }

        protected virtual void InjectType(TypeDefinition typeDefinition)
        {
            InjectMatchedMethod(typeDefinition);
            InjectMethod(typeDefinition);
            InjectProperty(typeDefinition);
        }

        private IEnumerable<TypeAttribute> GetMatchedTypeAttributeList(IEnumerable<TypeDefinition> typeDefinitions)
        {
            return typeDefinitions.Where(t => t.CustomAttributes.Any(c => IsSubclassOf(c.AttributeType.Resolve(), t.Module.Import(typeof(MatchedMethodInterceptionAttribute)).Resolve(), false)))
                                  .Select(t => new TypeAttribute
                                  {
                                      Type = t,
                                      Attributes = t.CustomAttributes.Where(c => IsSubclassOf(c.AttributeType.Resolve(), t.Module.Import(typeof(MatchedMethodInterceptionAttribute)).Resolve(), false)).ToList()
                                  });
        }

        protected virtual void InjectMatchedMethod(TypeDefinition typeDefinition)
        {
            var attributes = typeDefinition.CustomAttributes.Where(c => IsSubclassOf(c.AttributeType.Resolve(), typeDefinition.Module.Import(typeof(MatchedMethodInterceptionAttribute)).Resolve(), false)).ToList();
            attributes.ForEach(attr =>
            {
                var methods = typeDefinition.Methods.Where(m => !m.IsSpecialName && !m.IsSetter && !m.IsGetter && MatchMethodInterception(attr.Properties.Single(p => p.Name == "Rule").Argument.Value.ToString(), m.Name)
                                                        && !typeDefinition.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName))
                                                    .ToList();
                methods.ForEach(
                       k =>
                       {
                           InjectMethodInternal(k, attr, typeDefinition, InjectionType.Class);
                       });
            });
        }

        protected virtual void InjectMethod(TypeDefinition typeDefinition)
        {
            var methods = typeDefinition.Methods.Where(t => !t.IsSpecialName && !t.IsSetter && !t.IsGetter).ToList();
            System.IO.File.AppendAllText("d:\\1.log", methods.Count.ToString()+ "\r\n");
            for (var i = methods.Count - 1; i >= 0; i--)
            {
                var method = methods[i];
                if (method.HasCustomAttributes && !method.CustomAttributes
                    .Any(t => t.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName))
                {
                    var customerAttributes = method.CustomAttributes
                        .Where(t => IsSubclassOf(t.AttributeType.Resolve(), typeDefinition.Module.Import(typeof(IMethodInterceptor)).Resolve(), true))
                        .Select(t =>
                            new
                            {
                                Attribute = t,
                                Order = t.Properties.Any(p => p.Name == "Order") ? (int)t.Properties.SingleOrDefault(p => p.Name == "Order").Argument.Value : int.MaxValue,
                            }).OrderBy(t => t.Order).Select(t => t.Attribute).ToList();
                    System.IO.File.AppendAllText("d:\\1.log", customerAttributes.Count.ToString() + "\r\n");
                    customerAttributes.ForEach(

                            t => InjectMethodInternal(method, t, typeDefinition, InjectionType.Method)
                        );

                }
            }
        }

        protected virtual void InjectProperty(TypeDefinition typeDefinition)
        {
            var propertys = typeDefinition.Properties.Where(t => !t.IsSpecialName).Where(t => t.HasCustomAttributes
                    && !t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName)).ToList();
            propertys.ForEach(
                p =>
                {
                    var customerAttributes = p.CustomAttributes.Where(k => IsSubclassOf(k.AttributeType.Resolve(), p.Module.Import(typeof(PropertyInterceptionAttribute)).Resolve(), false))
                      .Select(t =>
                          new
                          {
                              Property = p,
                              Attribute = t,
                              Order = t.Properties.Any(tp => tp.Name == "Order") ? (int)t.Properties.SingleOrDefault(tp => tp.Name == "Order").Argument.Value : int.MaxValue,
                              InterceptionType = t.Properties.Any(tp => tp.Name == "InterceptionType") ? (PropertyInterceptionType)t.Properties.SingleOrDefault(tp => tp.Name == "InterceptionType").Argument.Value : PropertyInterceptionType.None,
                          }).OrderBy(t => t.Order).ToList();

                    customerAttributes.ForEach(t =>
                    {
                        if (t.InterceptionType == PropertyInterceptionType.Get && t.Property.GetMethod != null)
                        {
                            InjectMethodInternal(t.Property.GetMethod, t.Attribute, typeDefinition, InjectionType.Property, t.Property);
                        }
                        else if (t.InterceptionType == PropertyInterceptionType.Set && t.Property.SetMethod != null)
                        {
                            InjectMethodInternal(t.Property.SetMethod, t.Attribute, typeDefinition, InjectionType.Property, t.Property);
                        }
                    });
                });
        }

        protected virtual void InjectMethodInternal(MethodDefinition method, CustomAttribute attribute, TypeDefinition type, InjectionType injectionType, PropertyDefinition property = null)
        {
            var il = method.Body.GetILProcessor();
            var module = method.Module;

            // Generate a cloned method
            var newMethod = GenerateMethod(method, module);

            // Mark the new method as compiler-generated 
            if (!newMethod.CustomAttributes.Any(c => c.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName))
            {
                newMethod.CustomAttributes.Add(new CustomAttribute(module.Import(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes))));
            }

            // Add the new method to type
            type.Methods.Add(newMethod);

            // Clear the original method body
            method.Body.Instructions.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.Variables.Clear();
            method.Body.Instructions.Add(il.Create(OpCodes.Nop));

            var varIMethodInject = new VariableDefinition(attribute.AttributeType);
            method.Body.Variables.Add(varIMethodInject);
            var varMethodBase = new VariableDefinition(module.Import(typeof(System.Reflection.MemberInfo)));
            method.Body.Variables.Add(varMethodBase);
            var varthis = new VariableDefinition(module.Import(typeof(System.Object)));
            method.Body.Variables.Add(varthis);
            var varparams = new VariableDefinition(module.Import(typeof(object[])));
            method.Body.Variables.Add(varparams);
            var varparams2 = new VariableDefinition(module.Import(typeof(object[])));
            method.Body.Variables.Add(varparams2);
            var varexception = new VariableDefinition(module.Import(typeof(System.Exception)));
            method.Body.Variables.Add(varexception);
            var varMethodExecutionEventArgs = new VariableDefinition(module.Import(typeof(MethodExecutionEventArgs)));
            method.Body.Variables.Add(varMethodExecutionEventArgs);
            var varflag = new VariableDefinition(module.Import(typeof(bool)));
            method.Body.Variables.Add(varflag);
            var varExceptionStrategy = new VariableDefinition(module.Import(typeof(ExceptionStrategy)));
            method.Body.Variables.Add(varExceptionStrategy);
            var vartypeArray = new VariableDefinition(module.Import(typeof(Type[])));
            method.Body.Variables.Add(vartypeArray);

            method.Body.InitLocals = false;




            var lastNop = new[]{  il.Create(OpCodes.Nop)            ,
                il.Create(OpCodes.Nop)            ,
                il.Create(OpCodes.Nop)            ,
                (method.ReturnType.FullName != "System.Void")?il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs):il.Create(OpCodes.Nop),
             };

            var lastLeaves = il.Create(OpCodes.Leave_S, lastNop[1]);

            var case1 = il.Create(OpCodes.Br_S, lastNop[0]);
            var case2 = il.Create(OpCodes.Rethrow);
            var case3 = il.Create(OpCodes.Ldloc_S, varMethodExecutionEventArgs);

            il.Append(new[]
             {
                 il.Create(OpCodes.Nop),
             });
            if (injectionType == InjectionType.Method || injectionType == InjectionType.Class)
            {
                il.Append(new[]
             {
                 il.Create(OpCodes.Ldtoken,type),
                 il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                 il.Create(OpCodes.Ldstr,method.Name),
                 il.Create(OpCodes.Ldc_I4,method.Parameters.Count),
                 il.Create(OpCodes.Newarr,module.Import(typeof(System.Type))),
                 il.Create(OpCodes.Stloc_S,vartypeArray),
                 il.Create(OpCodes.Ldloc_S,vartypeArray),

             });

                var i = 0;
                method.Parameters.ToList().ForEach(t =>
                {

                    il.Append(new[]
                 {
                     il.Create(OpCodes.Ldc_I4,i++),
                     il.Create(OpCodes.Ldtoken,t.ParameterType),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                     il.Create(OpCodes.Stelem_Ref),
                     il.Create(OpCodes.Ldloc_S, vartypeArray),
                 });
                });

                il.Append(new[]
                 {

                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetMethod",new Type[]{typeof(string),typeof(Type[])}))),
                     il.Create(OpCodes.Stloc_S,varMethodBase),
                 });
            }
            else if (injectionType == InjectionType.Property)
            {
                il.Append(new[]
                 {
                     il.Create(OpCodes.Ldtoken,type),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                     il.Create(OpCodes.Ldstr,property.Name),
                     il.Create(OpCodes.Ldtoken,method.IsGetter?method.ReturnType:method.Parameters[0].ParameterType),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetProperty",new Type[]{typeof(string),typeof(Type)}))),
                     il.Create(OpCodes.Stloc_S,varMethodBase),
                 });
            }

            if (injectionType == InjectionType.Method || injectionType == InjectionType.Property)
            {
                il.Append(il.Create(OpCodes.Ldloc_S, varMethodBase));
            }
            else if (injectionType == InjectionType.Class)
            {
                il.Append(new[]
                {
                     il.Create(OpCodes.Ldtoken,type ),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                });
            }

            il.Append(new[]
             {
                 il.Create(OpCodes.Ldtoken,attribute.AttributeType ),
                 il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                 il.Create(OpCodes.Ldc_I4_0),
                 il.Create(OpCodes.Callvirt,module.Import(typeof(System.Reflection.MemberInfo).GetMethod("GetCustomAttributes",new Type[]{typeof(System.Type),typeof(bool)}))),
                 il.Create(OpCodes.Ldc_I4_0),
                 il.Create(OpCodes.Ldelem_Ref),
                 il.Create(OpCodes.Isinst,attribute.AttributeType),
                 il.Create(OpCodes.Stloc_S,varIMethodInject),
                 il.Create(OpCodes.Nop),
            });

            if (!method.IsStatic)
            {
                il.Append(new[] {
                 il.Create(OpCodes.Ldarg_S,method.Body.ThisParameter),
              });
            }
            else
            {
                il.Append(new[] {
                  il.Create(OpCodes.Ldnull),
              });
            }

            il.Append(new[] {
                il.Create(OpCodes.Stloc_S,varthis),
                il.Create(OpCodes.Ldc_I4,method.Parameters.Count),
                il.Create(OpCodes.Newarr,module.Import(typeof(object))),
                il.Create(OpCodes.Stloc_S,varparams2),
                il.Create(OpCodes.Ldloc_S,varparams2),
            });

            var j = 0;
            method.Parameters.ToList().ForEach(t =>
            {
                il.Append(new[] {
                    il.Create(OpCodes.Ldc_I4,j++),
                    il.Create(OpCodes.Ldarg_S, t),
                    il.Create(OpCodes.Box,t.ParameterType),
                    il.Create(OpCodes.Stelem_Ref),
                    il.Create(OpCodes.Ldloc_S,varparams2)
                });
            });


            il.Append(new[] {

                 il.Create(OpCodes.Stloc_S,varparams),
                 il.Create(OpCodes.Ldloc_S,varMethodBase),
                 il.Create(OpCodes.Ldloc_S,varthis),
                 il.Create(OpCodes.Ldloc_S,varparams),
            });

            if (method.ReturnType.FullName != "System.Void")
            {
                if (method.ReturnType.IsValueType)
                {

                    il.Append(new[] {
                        il.Create(OpCodes.Ldstr, method.ReturnType.FullName),
                    });

                }
                else
                {
                    il.Append(new[] {
                        il.Create(OpCodes.Ldnull ),
                    });

                }
            }

            il.Append(new[] {
                 il.Create(OpCodes.Newobj,module.Import(typeof(MethodExecutionEventArgs).GetConstructor(
                      new Type[] { typeof(System.Reflection.MethodBase), typeof(object), typeof(object[]),typeof(string) }))),
                il.Create(OpCodes.Stloc_S,varMethodExecutionEventArgs),


            });

            //if (method.ReturnType.FullName != "System.Void")
            //{
            //    ILProcessorExsions.Append(il, new[] {                                         
            //      il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
            //    });

            //    if (method.ReturnType.IsValueType)
            //    {
            //        //method.Body.InitLocals = false;
            //        var varreturnValueDefault = new VariableDefinition(method.ReturnType);
            //        method.Body.Variables.Add(varreturnValueDefault);
            //        ILProcessorExsions.Append(il, new[] { 
            //        il.Create(OpCodes.Ldloc_S,varreturnValueDefault),
            //        il.Create(OpCodes.Initobj,method.ReturnType ),
            //        il.Create(OpCodes.Ldloc_S,varreturnValueDefault),
            //        il.Create(OpCodes.Box,method.ReturnType),                     
            //    });
            //    }
            //    else
            //    {
            //        ILProcessorExsions.Append(il, new[] {                     
            //            il.Create(OpCodes.Ldnull ),                            
            //        });

            //    }                

            //    ILProcessorExsions.Append(il, new[] { 
            //        il.Create(OpCodes.Callvirt,module.Import(typeof(Green.AOP.MethodExecutionEventArgs).GetMethod("set_ReturnValue",new Type[]{typeof(System.Object)}))),                 
            //    });
            //}



            il.Append(new[] {
                 il.Create(OpCodes.Nop),
                il.Create(OpCodes.Ldloc_S,varIMethodInject),
                il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInterceptor).GetMethod("BeforeExecute",new Type[]{typeof(MethodExecutionEventArgs)}))),
                il.Create(OpCodes.Ldc_I4_0),
                il.Create(OpCodes.Ceq),
                il.Create(OpCodes.Stloc_S,varflag),
                il.Create(OpCodes.Ldloc_S,varflag),
                il.Create(OpCodes.Brtrue_S,lastNop[3]),
                il.Create(OpCodes.Nop),
              });
            var trySatrt = il.Create(OpCodes.Nop);
            il.Append(new[] {
                 trySatrt,
                 il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
            });

            if (!method.IsStatic)
            {
                method.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0));//Load this;
            }
            method.Parameters.ToList().ForEach(t => { method.Body.Instructions.Add(il.Create(OpCodes.Ldarg_S, t)); });

            method.Body.Instructions.Add(il.Create(OpCodes.Call, newMethod));
            if (method.ReturnType.FullName != "System.Void")
            {
                il.Append(new[]
                  {
                      il.Create(OpCodes.Box,method.ReturnType),
                      il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("set_ReturnValue",new Type[]{typeof(System.Object)}))),
                      il.Create(OpCodes.Nop),
                  });

            }
            else
            {
                method.Body.Instructions.Add(il.Create(OpCodes.Nop));
            }

            var tryEnd = il.Create(OpCodes.Stloc_S, varexception);
            il.Append(new[]
              {
                  il.Create(OpCodes.Ldloc_S,varIMethodInject),
                  il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                  il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInterceptor).GetMethod("AfterExecute",new Type[]{typeof(MethodExecutionEventArgs)}))),
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Leave_S,lastNop[1]),
                  tryEnd,
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                  il.Create(OpCodes.Ldloc_S,varexception),
                  il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("set_Exception",new Type[]{typeof(System.Exception)}))),
                  il.Create(OpCodes.Nop),
                  il.Create(OpCodes.Ldloc_S,varIMethodInject),
                  il.Create(OpCodes.Ldloc_S,varMethodExecutionEventArgs),
                  il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInterceptor).GetMethod("OnExecption",new Type[]{typeof(MethodExecutionEventArgs)}))),
                  il.Create(OpCodes.Stloc_S,varExceptionStrategy),
                  il.Create(OpCodes.Ldloc_S,varExceptionStrategy),
                  il.Create(OpCodes.Switch,new []{case1,case2,case3}),
                  il.Create(OpCodes.Br_S,lastNop[0]),
                 case1,
                 case2,
                 case3,
                 il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("get_Exception",new Type[]{}))),
                 il.Create(OpCodes.Throw),
              });

            il.Append(new[] {
                 lastNop[0],
                 lastLeaves,
                 lastNop[1],
                 lastNop[2],
                 lastNop[3],
             });
            if (method.ReturnType.FullName != "System.Void")
            {
                var varreturnValue = new VariableDefinition(method.ReturnType);
                method.Body.Variables.Add(varreturnValue);
                var lastreturn = il.Create(OpCodes.Ldloc_S, varreturnValue);
                il.Append(new[] {
                     il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("get_ReturnValue",new Type[]{}))),
                     il.Create(OpCodes.Unbox_Any,method.ReturnType),
                     il.Create(OpCodes.Stloc_S,varreturnValue),
                     il.Create(OpCodes.Br_S,lastreturn),
                     lastreturn,
                });
            }

            method.Body.Instructions.Add(il.Create(OpCodes.Ret));
            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                HandlerEnd = lastNop[1],
                HandlerStart = tryEnd,
                TryEnd = tryEnd,
                TryStart = trySatrt,
                CatchType = module.Import(typeof(System.Exception))
            });
        }


        private MethodDefinition GenerateMethod(MethodDefinition method, ModuleDefinition module)
        {
            var newMethod = new MethodDefinition(method.Name + (Guid.NewGuid().ToString().Replace("-", "_")), method.Attributes, method.ReturnType)
            {
                IsPrivate = true,
                IsStatic = method.IsStatic
            };
            method.CustomAttributes.ToList().ForEach(t => { newMethod.CustomAttributes.Add(t); });
            method.Body.Instructions.ToList().ForEach(t => { newMethod.Body.Instructions.Add(t); });
            method.Body.Variables.ToList().ForEach(t => { newMethod.Body.Variables.Add(t); });
            method.Body.ExceptionHandlers.ToList().ForEach(t => { newMethod.Body.ExceptionHandlers.Add(t); });
            method.Parameters.ToList().ForEach(t => { newMethod.Parameters.Add(t); });
            method.GenericParameters.ToList().ForEach(t => { newMethod.GenericParameters.Add(t); });

            newMethod.Body.LocalVarToken = method.Body.LocalVarToken;
            newMethod.Body.InitLocals = method.Body.InitLocals;
            return newMethod;
        }

        public static bool IsSubclassOf(TypeDefinition type, TypeDefinition baseType, bool isInterface)
        {
            if (type == null || baseType == null)
                return false;
            if (type.FullName == typeof(object).FullName)
            {
                return false;
            }
            if (isInterface)
            {
                if (type.Interfaces.Any(t => t.FullName == baseType.FullName))
                {
                    return true;
                }
            }
            else
            {
                if (type.FullName == baseType.FullName)
                {
                    return true;
                }
            }
            return IsSubclassOf(type.BaseType.Resolve(), baseType, isInterface);
        }

        public static bool MatchMethodInterception(string Rule, string method)
        {
            var re = Rule.Replace("*", @"\w*").Replace("-", @"\w");
            return System.Text.RegularExpressions.Regex.IsMatch(method, string.Format("^{0}$", re), System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        }
    }

    public class TypeAttribute
    {
        public TypeDefinition Type { get; set; }
        public List<CustomAttribute> Attributes { get; set; }

    }

    public enum InjectionType
    {
        Method,
        Class,
        Property,
        Field
    }
}
