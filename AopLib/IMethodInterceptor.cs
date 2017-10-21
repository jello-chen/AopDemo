namespace AopLib
{
    public interface IMethodInterceptor
    {
        int Order { get; set; }
        bool BeforeExecute(MethodExecutionEventArgs args);
        ExceptionStrategy OnExecption(MethodExecutionEventArgs args);
        void AfterExecute(MethodExecutionEventArgs args);
    }
}
