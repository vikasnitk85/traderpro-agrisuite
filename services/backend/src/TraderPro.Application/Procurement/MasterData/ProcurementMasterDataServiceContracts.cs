using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.MasterData;

namespace TraderPro.Application.Procurement.MasterData;

public interface IProcurementMasterDataService
{
    Task<IdempotentCommandResult<ReceivingVehicleResult>> CreateVehicleAsync(
        CreateReceivingVehicleCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ReceivingVehicleResult>> UpdateVehicleAsync(
        UpdateReceivingVehicleCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ReceivingVehicleResult>> SetVehicleStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ReceivingVehicleResult> GetVehicleAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<ReceivingVehicleResult>> ListVehiclesAsync(
        MasterListQuery query,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<BagTypeResult>> CreateBagTypeAsync(
        CreateBagTypeCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<BagTypeResult>> UpdateBagTypeAsync(
        UpdateBagTypeCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<BagTypeResult>> SetBagTypeStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<BagTypeResult> GetBagTypeAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<BagTypeResult>> ListBagTypesAsync(
        MasterListQuery query,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        CreateWeightPolicyAsync(
            CreateWeightProcessingPolicyCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        UpdateWeightPolicyAsync(
            UpdateWeightProcessingPolicyCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        SetWeightPolicyStatusAsync(
            MasterStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<WeightProcessingPolicyResult> GetWeightPolicyAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<WeightProcessingPolicyResult>> ListWeightPoliciesAsync(
        MasterListQuery query,
        CancellationToken cancellationToken);

    Task<CompanyProcurementSettingsResult> GetSettingsAsync(
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<CompanyProcurementSettingsResult>>
        ConfigureSettingsAsync(
            ConfigureCompanyProcurementSettingsCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);
}
