namespace Factos;

[AttributeUsage(AttributeTargets.Method)]
public class SkipAttribute : Attribute
{
    public string Reason { get; private set; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }
}