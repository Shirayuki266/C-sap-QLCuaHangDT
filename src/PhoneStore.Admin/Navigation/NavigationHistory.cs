namespace PhoneStore.Admin.Navigation;

// Deliberately independent of WinForms; the Ubuntu runner compiles this same source.
public sealed class NavigationHistory<T> where T : class
{
    private readonly List<T> entries = [];
    public NavigationHistory(int capacity = 30)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }
    public int Capacity { get; }
    public IReadOnlyList<T> Entries => entries.AsReadOnly();
    public int Index { get; private set; } = -1;
    public T? Current => Index < 0 ? null : entries[Index];
    public bool CanBack => Index > 0;
    public bool CanForward => Index >= 0 && Index + 1 < entries.Count;
    public IReadOnlyList<T> Push(T entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var removed = entries.Skip(Index + 1).ToList();
        entries.RemoveRange(Index + 1, entries.Count - Index - 1);
        entries.Add(entry);
        if (entries.Count > Capacity) { removed.Add(entries[0]); entries.RemoveAt(0); }
        Index = entries.Count - 1;
        return removed;
    }
    public bool GoTo(int index)
    {
        if (index < 0 || index >= entries.Count || index == Index) return false;
        Index = index; return true;
    }
    public bool Back() => CanBack && GoTo(Index - 1);
    public bool Forward() => CanForward && GoTo(Index + 1);
    public IReadOnlyList<T> Clear()
    {
        var removed = entries.ToArray(); entries.Clear(); Index = -1; return removed;
    }
    public IReadOnlyList<T> Reset(T entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var removed = Clear(); entries.Add(entry); Index = 0; return removed;
    }
}
