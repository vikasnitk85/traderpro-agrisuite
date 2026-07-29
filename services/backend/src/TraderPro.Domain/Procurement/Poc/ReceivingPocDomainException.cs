namespace TraderPro.Domain.Procurement.Poc;

public sealed class ReceivingPocDomainException(
    string code,
    string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
