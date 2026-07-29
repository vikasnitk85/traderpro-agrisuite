using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Procurement.Poc;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Persistence;
using TraderPro.Infrastructure.Tenancy;

namespace TraderPro.Infrastructure.Modules.Procurement.Poc;

internal sealed class TemporaryDeviceContextResolver(
    TraderProDbContext dbContext,
    CurrentWorkspaceAccessor currentWorkspace,
    CurrentDeviceAccessor currentDevice) : ITemporaryDeviceContextResolver
{
    public async Task BindAsync(
        Guid workspaceId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var workspaceExists = await dbContext.Workspaces
            .AsNoTracking()
            .AnyAsync(
                workspace => workspace.Id == workspaceId,
                cancellationToken);
        if (!workspaceExists)
        {
            throw new ApplicationProblemException(
                "DEVICE_CONTEXT_INVALID",
                "The temporary workspace/device context is invalid.",
                ApplicationErrorCategory.Validation);
        }

        var device = await dbContext.Devices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(candidate => candidate.Id == deviceId)
            .Select(candidate => new
            {
                candidate.WorkspaceId,
                candidate.Status,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (device is null)
        {
            throw new ApplicationProblemException(
                "DEVICE_NOT_FOUND",
                "The requested device was not found.",
                ApplicationErrorCategory.NotFound);
        }

        if (device.WorkspaceId != workspaceId)
        {
            throw new ApplicationProblemException(
                "DEVICE_WORKSPACE_MISMATCH",
                "The device does not belong to the requested workspace.",
                ApplicationErrorCategory.Conflict);
        }

        if (device.Status != DeviceStatus.Active)
        {
            throw new ApplicationProblemException(
                "DEVICE_NOT_ACTIVE",
                "The requested device is not active.",
                ApplicationErrorCategory.Conflict);
        }

        currentWorkspace.SetWorkspace(workspaceId);
        currentDevice.SetDevice(deviceId);
    }
}
