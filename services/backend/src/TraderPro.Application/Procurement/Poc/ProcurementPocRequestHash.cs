using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TraderPro.Application.Procurement.Poc;

public static class ProcurementPocRequestHash
{
    public static string ForStart(
        Guid deviceId,
        string operationType,
        Guid aggregateId,
        long localSequence,
        long? expectedCloudVersion,
        string? temporaryReference,
        DateTimeOffset? createdAtDeviceUtc)
    {
        return Compute(
            MobileOperationHeader(deviceId, operationType, aggregateId) +
            $"localSequence:{localSequence}\n" +
            OptionalVersion(expectedCloudVersion) +
            Text("temporaryReference", temporaryReference?.Trim()) +
            Timestamp("createdAtDeviceUtc", createdAtDeviceUtc));
    }

    public static string ForRecordEntry(
        Guid deviceId,
        string operationType,
        Guid aggregateId,
        long localSequence,
        long? expectedCloudVersion,
        Guid leaseId,
        string productReference,
        string bagTypeReference,
        int bagCount,
        string rawWeightKg,
        string processedWeightKg,
        string displayWeightKg,
        int decimalPlaces,
        string processingMethod,
        string weightSource,
        DateTimeOffset capturedAtDeviceUtc)
    {
        return Compute(
            MobileOperationHeader(deviceId, operationType, aggregateId) +
            $"localSequence:{localSequence}\n" +
            OptionalVersion(expectedCloudVersion) +
            $"leaseId:{leaseId:D}\n" +
            Text("productReference", productReference.Trim()) +
            Text("bagTypeReference", bagTypeReference.Trim()) +
            $"bagCount:{bagCount}\n" +
            Text("rawWeightKg", rawWeightKg) +
            Text("processedWeightKg", processedWeightKg) +
            Text("displayWeightKg", displayWeightKg) +
            $"decimalPlaces:{decimalPlaces}\n" +
            Text("processingMethod", processingMethod) +
            Text("weightSource", weightSource.Trim()) +
            Timestamp("capturedAtDeviceUtc", capturedAtDeviceUtc));
    }

    public static string ForSubmit(
        Guid deviceId,
        string operationType,
        Guid aggregateId,
        long localSequence,
        long? expectedCloudVersion,
        Guid leaseId)
    {
        return Compute(
            MobileOperationHeader(deviceId, operationType, aggregateId) +
            $"localSequence:{localSequence}\n" +
            OptionalVersion(expectedCloudVersion) +
            $"leaseId:{leaseId:D}");
    }

    public static string ForHeartbeat(
        Guid deviceId,
        Guid sessionId,
        Guid leaseId)
    {
        return Compute(
            Header(
                ProcurementPocCommandTypes.Heartbeat,
                deviceId,
                sessionId) +
            $"leaseId:{leaseId:D}");
    }

    public static string ForApprove(
        Guid deviceId,
        Guid sessionId,
        long expectedVersion)
    {
        return Compute(
            Header(
                ProcurementPocCommandTypes.Approve,
                deviceId,
                sessionId) +
            $"expectedVersion:{expectedVersion}");
    }

    public static string ForFinalize(
        Guid deviceId,
        Guid sessionId,
        long expectedVersion)
    {
        return Compute(
            Header(
                ProcurementPocCommandTypes.Finalize,
                deviceId,
                sessionId) +
            $"expectedVersion:{expectedVersion}");
    }

    private static string MobileOperationHeader(
        Guid deviceId,
        string operationType,
        Guid aggregateId)
    {
        return Header(
                ProcurementPocCommandTypes.MobileSyncOperation,
                deviceId,
                aggregateId) +
            Text("operationType", operationType);
    }

    private static string Header(
        string commandType,
        Guid deviceId,
        Guid aggregateId)
    {
        return $"commandType:{commandType}\n" +
            $"deviceId:{deviceId:D}\n" +
            $"aggregateId:{aggregateId:D}\n";
    }

    private static string OptionalVersion(long? value)
    {
        return value is null
            ? "expectedCloudVersion:null\n"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"expectedCloudVersion:{value.Value}\n");
    }

    private static string Text(string name, string? value)
    {
        if (value is null)
        {
            return $"{name}:null\n";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{name}:{Encoding.UTF8.GetByteCount(value)}:{value}\n");
    }

    private static string Timestamp(
        string name,
        DateTimeOffset? value)
    {
        return value is null
            ? $"{name}:null\n"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{name}:{value.Value:O}\n");
    }

    private static string Compute(string canonicalRequest)
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
    }
}
