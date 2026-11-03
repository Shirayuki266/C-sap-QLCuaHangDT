namespace PhoneStore.DemoData.Contracts;

// Input facts shared with the independent verifier, not generation algorithms or calculated balances.
public sealed record ProductLifecycle(string Code, DateTime LaunchAt, DateTime? RetiredAt);
public sealed record DatasetContext(int Seed, DateOnly AsOf, string Scale, DateTime Start, DateTime Cutoff,
    ProductLifecycle[] Products);
