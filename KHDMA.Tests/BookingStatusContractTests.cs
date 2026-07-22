using KHDMA.Application.Common;
using KHDMA.Domain.Enums;

namespace KHDMA.Tests;

/// <summary>
/// Guards the wire and storage contract around <see cref="BookingStatus"/>.
/// </summary>
public class BookingStatusContractTests
{
    /// <summary>
    /// Existing rows store the integer value. Reordering or inserting a member
    /// silently reassigns the meaning of every historical booking - a completed
    /// job could read back as cancelled. This test is the tripwire.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, 0)]
    [InlineData(BookingStatus.Dispatching, 1)]
    [InlineData(BookingStatus.Accepted, 2)]
    [InlineData(BookingStatus.EnRoute, 3)]
    [InlineData(BookingStatus.Arrived, 4)]
    [InlineData(BookingStatus.InProgress, 5)]
    [InlineData(BookingStatus.Completed, 6)]
    [InlineData(BookingStatus.Cancelled, 7)]
    [InlineData(BookingStatus.NoProviderFound, 8)]
    [InlineData(BookingStatus.Failed, 9)]
    public void StatusValues_AreStable(BookingStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Theory]
    [InlineData(PayoutStatus.Requested, 0)]
    [InlineData(PayoutStatus.Approved, 1)]
    [InlineData(PayoutStatus.Paid, 2)]
    [InlineData(PayoutStatus.Rejected, 3)]
    public void PayoutStatusValues_AreStable(PayoutStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    /// <summary>
    /// Every status ships bilingual labels; a missing one would render the raw
    /// enum name to an Arabic-speaking user.
    /// </summary>
    [Fact]
    public void EveryStatus_HasBothLabels()
    {
        foreach (var status in Enum.GetValues<BookingStatus>())
        {
            var en = BookingStatusLabels.En(status);
            var ar = BookingStatusLabels.Ar(status);

            Assert.False(string.IsNullOrWhiteSpace(en), $"{status} has no English label");
            Assert.False(string.IsNullOrWhiteSpace(ar), $"{status} has no Arabic label");
            Assert.NotEqual(en, ar);
        }
    }

    /// <summary>
    /// Pending reads as "Paid" to the customer: by the time they see it, payment
    /// has been taken and the search has not started. Documented in
    /// API_CONTRACTS.md section 5 and easy to "fix" by mistake.
    /// </summary>
    [Fact]
    public void PendingLabel_IsPaid_NotPending()
    {
        Assert.Equal("Paid", BookingStatusLabels.En(BookingStatus.Pending));
    }
}
