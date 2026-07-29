using TraderPro.Application.Common.Tenancy;

namespace TraderPro.Infrastructure.Tenancy;

public sealed class CurrentDeviceAccessor : ICurrentDeviceAccessor
{
    public Guid? DeviceId { get; private set; }

    public void SetDevice(Guid deviceId)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty device identifier is required.",
                nameof(deviceId));
        }

        if (DeviceId is not null && DeviceId != deviceId)
        {
            throw new InvalidOperationException(
                "The current scope is already bound to another device.");
        }

        DeviceId = deviceId;
    }
}
