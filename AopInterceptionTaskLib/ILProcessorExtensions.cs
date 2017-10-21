using Mono.Cecil.Cil;
using System;

namespace AopInterceptionTaskLib
{
    public static class ILProcessorExtensions
    {
        public static void InsertBefore(this ILProcessor iLProcessor, Instruction target, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => iLProcessor.InsertBefore(target, t));
            }
        }

        public static void InsertAfter(this ILProcessor iLProcessor, Instruction target, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => { iLProcessor.InsertAfter(target, t); target = t; });
            }
        }

        public static void Append(this ILProcessor iLProcessor, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => { iLProcessor.Append(t); });
            }
        }
    }
}
