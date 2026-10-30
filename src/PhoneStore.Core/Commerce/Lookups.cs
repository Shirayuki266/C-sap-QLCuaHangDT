namespace PhoneStore.Core.Commerce;
public sealed record LookupInput(string? Name, bool Active = true);
public sealed record LookupView(int Id, string Name, bool Active)
{
    public LookupView() : this(0, "", false) { }
}
