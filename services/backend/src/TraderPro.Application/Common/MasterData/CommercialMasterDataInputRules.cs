using System.Globalization;
using System.Text.RegularExpressions;
using TraderPro.Application.Common.Errors;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Procurement.MasterData;

namespace TraderPro.Application.Common.MasterData;

public static partial class CommercialMasterDataInputRules
{
    public static string RequireIdempotencyKey(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw Validation(
                "IDEMPOTENCY_KEY_REQUIRED",
                "An Idempotency-Key header is required.");
        }

        if (!IdempotencyKeyPattern().IsMatch(value))
        {
            throw Validation(
                "IDEMPOTENCY_KEY_INVALID",
                "The idempotency key must contain 1 to 200 letters, digits, dots, underscores, colons, or hyphens.");
        }

        return value;
    }

    public static long RequireExpectedVersion(long? value)
    {
        if (value is null or <= 0)
        {
            throw Validation(
                "MASTER_EXPECTED_VERSION_REQUIRED",
                "X-Expected-Version must be a positive integer.");
        }

        return value.Value;
    }

    public static MasterListQuery CreateListQuery(
        string? status,
        string? search,
        string? cursor,
        int limit)
    {
        var parsedStatus = status switch
        {
            null or "" => MasterStatusFilter.Active,
            "Active" => MasterStatusFilter.Active,
            "Inactive" => MasterStatusFilter.Inactive,
            "All" => MasterStatusFilter.All,
            _ => throw Validation(
                "MASTER_STATUS_INVALID",
                "Status must be Active, Inactive, or All.",
                "status"),
        };
        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim().ToUpperInvariant();
        if (normalizedSearch?.Length > 100 ||
            normalizedSearch?.Any(char.IsControl) == true)
        {
            throw Validation(
                "MASTER_SEARCH_INVALID",
                "Search text cannot exceed 100 safe characters.",
                "search");
        }

        if (limit is < 1 or > 100)
        {
            throw Validation(
                "MASTER_PAGE_SIZE_INVALID",
                "Limit must be between 1 and 100.",
                "limit");
        }

        return new MasterListQuery(
            parsedStatus,
            normalizedSearch,
            string.IsNullOrWhiteSpace(cursor) ? null : cursor,
            limit);
    }

    public static string CanonicalRequiredText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static string? CanonicalOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string CanonicalRegistrationDisplay(string? value)
    {
        try
        {
            return MasterDataValueRules.RequiredText(
                value,
                64,
                "registrationNumber",
                "RECEIVING_VEHICLE_INVALID");
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }
    }

    public static bool RequireBagReturnability(bool? value)
    {
        if (value is null)
        {
            throw Validation(
                "BAG_TYPE_INVALID",
                "isReturnable must be explicitly supplied as true or false.",
                "isReturnable");
        }

        return value.Value;
    }

    public static BusinessLocationType ParseLocationType(string? value)
    {
        return value switch
        {
            "Warehouse" => BusinessLocationType.Warehouse,
            "Yard" => BusinessLocationType.Yard,
            "Mill" => BusinessLocationType.Mill,
            "Office" => BusinessLocationType.Office,
            "Other" => BusinessLocationType.Other,
            _ => throw Validation(
                "BUSINESS_LOCATION_INVALID",
                "Location type must be Warehouse, Yard, Mill, Office, or Other.",
                "locationType"),
        };
    }

    public static ReceivingVehicleType ParseVehicleType(string? value)
    {
        return value switch
        {
            "Truck" => ReceivingVehicleType.Truck,
            "Tractor" => ReceivingVehicleType.Tractor,
            "Van" => ReceivingVehicleType.Van,
            "Other" => ReceivingVehicleType.Other,
            _ => throw Validation(
                "RECEIVING_VEHICLE_INVALID",
                "Vehicle type must be Truck, Tractor, Van, or Other.",
                "vehicleType"),
        };
    }

    public static BagConstructionClass ParseConstructionClass(string? value)
    {
        return value switch
        {
            "Jute" => BagConstructionClass.Jute,
            "SinglePlastic" => BagConstructionClass.SinglePlastic,
            "DoublePlastic" => BagConstructionClass.DoublePlastic,
            "Other" => BagConstructionClass.Other,
            _ => throw Validation(
                "BAG_TYPE_INVALID",
                "Construction class must be Jute, SinglePlastic, DoublePlastic, or Other.",
                "constructionClass"),
        };
    }

    public static decimal ParseTareWeight(string? value)
    {
        if (value is null ||
            !TareWeightPattern().IsMatch(value) ||
            !decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            throw Validation(
                "BAG_TYPE_TARE_WEIGHT_INVALID",
                "Standard tare weight must be a non-negative decimal string with at most 14 integer and 6 fractional digits.",
                "standardTareWeightKg");
        }

        try
        {
            return MasterDataValueRules.ValidateTareWeight(parsed);
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }
    }

    public static WeightProcessingMethod ParseProcessingMethod(string? value)
    {
        return value switch
        {
            "Standard" => WeightProcessingMethod.Standard,
            "Floor" => WeightProcessingMethod.Floor,
            "Ceiling" => WeightProcessingMethod.Ceiling,
            _ => throw Validation(
                "WEIGHT_POLICY_METHOD_INVALID",
                "Processing method must be Standard, Floor, or Ceiling.",
                "processingMethod"),
        };
    }

    public static VehicleSelectionMode ParseVehicleSelectionMode(string? value)
    {
        return value switch
        {
            "Optional" => VehicleSelectionMode.Optional,
            "Disabled" => VehicleSelectionMode.Disabled,
            _ => throw Validation(
                "PROCUREMENT_SETTINGS_INVALID",
                "Vehicle selection mode must be Optional or Disabled.",
                "vehicleSelectionMode"),
        };
    }

    private static ApplicationProblemException Validation(
        string code,
        string message,
        string? field = null)
    {
        return new ApplicationProblemException(
            code,
            message,
            ApplicationErrorCategory.Validation,
            fieldErrors: field is null
                ? []
                : [new ApplicationFieldError(field, code, message)]);
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,200}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdempotencyKeyPattern();

    [GeneratedRegex(
        "^[0-9]{1,14}(\\.[0-9]{1,6})?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TareWeightPattern();
}
