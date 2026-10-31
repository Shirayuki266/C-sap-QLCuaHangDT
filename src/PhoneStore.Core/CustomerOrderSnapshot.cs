using PhoneStore.Core.Commerce;

namespace PhoneStore.Core;

// Customer presentation projection; internal account/history actors and command keys never cross this boundary.
public sealed record CustomerOrderSnapshot(long Id, string Code, string Recipient, string Phone, string Address,
    string Status, string Payment, decimal Total, DateTimeOffset CreatedAt,
    List<OrderLineView> Lines, List<CustomerOrderEvent> Timeline, string Version);
public sealed record CustomerOrderEvent(string? From, string To, DateTimeOffset At, string? Reason);
public sealed record CustomerOrderSummary(long Id, string Code, string Recipient, string Status, string Payment, decimal Total, DateTimeOffset CreatedAt);
