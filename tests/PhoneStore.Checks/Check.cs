namespace PhoneStore.Checks;

internal static class Check
{
    private static int count;

    public static async Task Run(string name, Func<Task> test)
    {
        await test();
        count++;
        Console.WriteLine($"PASS {name}");
    }

    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void Reject<T>(Action test) where T : Exception
    {
        try { test(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    public static void Summary() => Console.WriteLine($"{count} checks passed.");
}
