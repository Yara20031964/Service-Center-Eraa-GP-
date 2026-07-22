using KHDMA.Application.Common;

namespace KHDMA.Tests;

/// <summary>
/// SRS 7.3 - contact details must not be exchanged in chat.
/// </summary>
/// <remarks>
/// The false-positive cases matter as much as the true positives: a filter that
/// blocks "I'll be there in 20 minutes, flat 15" is one users route around by
/// leaving the platform, which is the exact outcome the rule exists to prevent.
/// </remarks>
public class PiiFilterTests
{
    [Theory]
    [InlineData("call me on 01012345678")]
    [InlineData("my number is 010 1234 5678")]
    [InlineData("reach me at 010-1234-5678")]
    [InlineData("0 1 0 1 2 3 4 5 6 7 8")]                 // spaced out to evade a naive regex
    [InlineData("phone: 010.1234.5678")]
    [InlineData("+20 101 234 5678")]
    [InlineData("رقمي ٠١٠١٢٣٤٥٦٧٨")]                      // Arabic-Indic digits
    public void ContainsPii_DetectsPhoneNumbers(string text)
    {
        Assert.True(PiiFilter.ContainsPii(text));
    }

    [Theory]
    [InlineData("email me at ahmed@example.com")]
    [InlineData("ahmed (at) example (dot) com")]
    [InlineData("Ahmed.Hassan+work@sub-domain.co.uk")]
    public void ContainsPii_DetectsEmailAddresses(string text)
    {
        Assert.True(PiiFilter.ContainsPii(text));
    }

    [Theory]
    [InlineData("lets talk on whatsapp")]
    [InlineData("add me on Telegram")]
    [InlineData("wa.me link coming")]
    public void ContainsPii_DetectsOffPlatformMessagingApps(string text)
    {
        Assert.True(PiiFilter.ContainsPii(text));
    }

    [Theory]
    [InlineData("I'll be there in 20 minutes")]
    [InlineData("The price is 250 EGP")]
    [InlineData("Apartment 15, floor 3, building 27")]
    [InlineData("See you at 10:30")]
    [InlineData("I need 2 pipes and 4 washers")]
    [InlineData("Order 2026-07-22 confirmed")]
    [InlineData("")]
    [InlineData(null)]
    public void ContainsPii_AllowsOrdinaryConversation(string? text)
    {
        Assert.False(PiiFilter.ContainsPii(text));
    }

    [Fact]
    public void ContainsPii_ExplainsWhyItBlocked()
    {
        PiiFilter.ContainsPii("call 01012345678", out var phoneReason);
        PiiFilter.ContainsPii("mail me at a@b.com", out var emailReason);

        // The reason is surfaced in the chat input bar, so it must name the
        // actual problem rather than say "invalid message".
        Assert.Contains("Phone", phoneReason);
        Assert.Contains("Email", emailReason);
    }
}
