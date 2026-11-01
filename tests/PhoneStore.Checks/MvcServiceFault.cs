using System.Reflection;
using PhoneStore.Core;

namespace PhoneStore.Checks;

internal sealed class MvcServiceFault
{
    public string? Method { get; private set; }
    private Exception? exception;
    public void Arm(string method,string code){Method=method;exception=new BusinessException(code,"Injected service validation QA");}
    public Exception? Take(string method)=>Method==method?Interlocked.Exchange(ref exception,null):null;
}
public class MvcServiceFaultProxy : DispatchProxy
{
    internal IStoreService Inner { get; set; }=null!;
    internal MvcServiceFault Fault { get; set; }=null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        var error=Fault.Take(method!.Name);
        if(error is not null)
        {
            if(method.ReturnType==typeof(Task))return Task.FromException(error);
            return typeof(Task).GetMethods().Single(m=>m.Name==nameof(Task.FromException)&&m.IsGenericMethod).MakeGenericMethod(method.ReturnType.GenericTypeArguments[0]).Invoke(null,[error]);
        }
        return method.Invoke(Inner,args);
    }
}
