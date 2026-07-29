namespace TraderPro.Domain.Procurement.Poc;

/// <summary>
/// Development-only states for the two-device procurement proof of concept.
/// These are not commercial Procurement document states.
/// </summary>
public enum ReceivingPocStatus : short
{
    ReceivingInProgress = 1,
    SubmittedForReview = 2,
    Approved = 3,
    Finalized = 4,
}
