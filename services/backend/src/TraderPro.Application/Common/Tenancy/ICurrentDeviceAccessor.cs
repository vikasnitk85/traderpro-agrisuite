namespace TraderPro.Application.Common.Tenancy;

/// <summary>
/// Temporary request-scoped device context for development/testing spikes.
/// This is not authentication or authorization.
/// </summary>
public interface ICurrentDeviceAccessor
{
    Guid? DeviceId { get; }
}
