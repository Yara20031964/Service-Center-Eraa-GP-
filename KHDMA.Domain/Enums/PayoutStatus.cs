namespace KHDMA.Domain.Enums
{
    /// <summary>APPEND ONLY - persisted as an integer.</summary>
    public enum PayoutStatus
    {
        Requested = 0,
        Approved = 1,
        Paid = 2,
        Rejected = 3,
    }
}
