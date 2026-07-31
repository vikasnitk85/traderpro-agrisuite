namespace TraderPro.Domain.Common.MasterData;

public sealed class MasterDataDomainException(
    string code,
    string message,
    string? field = null) : Exception(message)
{
    public string Code { get; } = code;

    public string? Field { get; } = field;
}
