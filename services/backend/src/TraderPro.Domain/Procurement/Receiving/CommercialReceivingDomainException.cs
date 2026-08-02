namespace TraderPro.Domain.Procurement.Receiving;

public sealed class CommercialReceivingDomainException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}

