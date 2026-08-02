namespace TraderPro.Application.Procurement.Receiving;

public sealed record CommercialReceivingOptions(int LeaseMinutes)
{
    public const int DefaultLeaseMinutes = 60;
    public const int HeartbeatTargetMinutes = 10;

    public static CommercialReceivingOptions Default { get; } =
        new(DefaultLeaseMinutes);

    public TimeSpan LeaseDuration => TimeSpan.FromMinutes(LeaseMinutes);
}

