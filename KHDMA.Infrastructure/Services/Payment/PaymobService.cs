using Application.DTOs.Payment;
using Domain.Common;
using KHDMA.Application.Interfaces.Payment;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentEntity = KHDMA.Domain.Entities.Payment;
namespace KHDMA.Infrastructure.Services.Payment;

public class PaymobService : IPaymobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    private string ApiKey => _config["Paymob:ApiKey"]!;
    private string IntegrationId => _config["Paymob:IntegrationId"]!;
    private string IframeId => _config["Paymob:IframeId"]!;
    private string BaseUrl => _config["Paymob:BaseUrl"]!;

    public PaymobService(
        IUnitOfWork unitOfWork,
        IConfiguration config,
        HttpClient httpClient)
    {
        _unitOfWork = unitOfWork;
        _config = config;
        _httpClient = httpClient;
    }

    // ── STEP 1+2+3: Initiate Payment ─────────────────────────
    public async Task<ApiResponse<PaymentKeyResponseDto>> InitiatePaymentAsync(
        PaymentInitDto dto, string customerId)
    {
        // Get booking
        var booking = await _unitOfWork.Repository<Booking>()
            .GetOneAsync(
                b => b.Id == dto.BookingId &&
                     b.CustomerId == customerId &&
                     b.Status == BookingStatus.Pending,
                includes: [b => b.Customer, b => b.Service]);

        if (booking is null)
            return ApiResponse<PaymentKeyResponseDto>.NotFound("Booking not found");

        var amountCents = (int)(booking.TotalPrice * 100);

        // Step 1: Auth
        var authToken = await GetAuthTokenAsync();
        if (authToken is null)
            return ApiResponse<PaymentKeyResponseDto>.Fail("Paymob auth failed");

        // Step 2: Register Order
        var orderId = await RegisterOrderAsync(authToken, amountCents, dto.BookingId);
        if (orderId is null)
            return ApiResponse<PaymentKeyResponseDto>.Fail("Paymob order registration failed");

        // Step 3: Get Payment Key
        var paymentKey = await GetPaymentKeyAsync(
            authToken, orderId, amountCents,
            booking.Customer.ApplicationUser.Email!,
            booking.Customer.ApplicationUser.FullName);

        if (paymentKey is null)
            return ApiResponse<PaymentKeyResponseDto>.Fail("Paymob payment key failed");

        // Create Payment record
        var payment = new PaymentEntity
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            CommissionAmount = booking.TotalPrice * 0.15m,
            ProviderEarning = booking.TotalPrice * 0.85m,
            PaymentStatus = PaymentStatus.Pending,
            TransactionReference = orderId
        };

        await _unitOfWork.Repository<PaymentEntity>().CreateAsync(payment);
        await _unitOfWork.CommitAsync();

        return ApiResponse<PaymentKeyResponseDto>.Ok(new PaymentKeyResponseDto
        {
            PaymentKey = paymentKey,
            IframeUrl = $"https://accept.paymob.com/api/acceptance/iframes/{IframeId}?payment_token={paymentKey}"
        });
    }

    // ── WEBHOOK ───────────────────────────────────────────────
    public async Task<ApiResponse<string>> HandleWebhookAsync(
        PaymentWebhookDto dto, string hmacSignature)
    {
        // Verify HMAC
        if (!VerifyHmac(dto, hmacSignature))
            return ApiResponse<string>.Fail("Invalid HMAC signature", 401);

        var payment = await _unitOfWork.Repository<PaymentEntity>()
            .GetOneAsync(p => p.TransactionReference == dto.Obj.Order);

        if (payment is null)
            return ApiResponse<string>.NotFound("Payment not found");

        if (dto.Obj.Is_Refund)
        {
            payment.PaymentStatus = PaymentStatus.Refunded;
        }
        else if (dto.Obj.Success)
        {
            payment.PaymentStatus = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            payment.TransactionReference = dto.Obj.Id;

            // Advance booking to Dispatching
            var booking = await _unitOfWork.Repository<Booking>()
                .GetOneAsync(b => b.Id == payment.BookingId);

            if (booking is not null)
                booking.Status = BookingStatus.Dispatching;
        }
        else
        {
            payment.PaymentStatus = PaymentStatus.Failed;
        }

        _unitOfWork.Repository<PaymentEntity>().Update(payment);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Webhook processed");
    }

    // ── REFUND ────────────────────────────────────────────────
    public async Task<ApiResponse<string>> RefundAsync(Guid paymentId)
    {
        var payment = await _unitOfWork.Repository<PaymentEntity>()
            .GetOneAsync(p => p.Id == paymentId &&
                              p.PaymentStatus == PaymentStatus.Paid);

        if (payment is null)
            return ApiResponse<string>.NotFound("Payment not found or not eligible for refund");

        var authToken = await GetAuthTokenAsync();
        if (authToken is null)
            return ApiResponse<string>.Fail("Paymob auth failed");

        var amountCents = (int)(payment.Amount * 100);

        var refundPayload = new
        {
            auth_token = authToken,
            transaction_id = payment.TransactionReference,
            amount_cents = amountCents
        };

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/acceptance/void_refund/refund",
            new StringContent(
                JsonSerializer.Serialize(refundPayload),
                Encoding.UTF8,
                "application/json"));

        if (!response.IsSuccessStatusCode)
            return ApiResponse<string>.Fail("Paymob refund failed");

        payment.PaymentStatus = PaymentStatus.Refunded;
        _unitOfWork.Repository<PaymentEntity>().Update(payment);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Refund processed successfully");
    }

    // ── Private Helpers ───────────────────────────────────────
    private async Task<string?> GetAuthTokenAsync()
    {
        var payload = new { api_key = ApiKey };
        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/auth/tokens",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString();
    }

    private async Task<string?> RegisterOrderAsync(
        string authToken, int amountCents, Guid bookingId)
    {
        var payload = new
        {
            auth_token = authToken,
            delivery_needed = false,
            amount_cents = amountCents,
            currency = "EGP",
            merchant_order_id = bookingId.ToString(),
            items = Array.Empty<object>()
        };

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/ecommerce/orders",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt32().ToString();
    }

    private async Task<string?> GetPaymentKeyAsync(
        string authToken, string orderId,
        int amountCents, string email, string fullName)
    {
        var nameParts = fullName.Split(' ');
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : nameParts[0];

        var payload = new
        {
            auth_token = authToken,
            amount_cents = amountCents,
            expiration = 3600,
            order_id = orderId,
            billing_data = new
            {
                first_name = firstName,
                last_name = lastName,
                email = email,
                phone_number = "N/A",
                apartment = "N/A",
                floor = "N/A",
                street = "N/A",
                building = "N/A",
                shipping_method = "N/A",
                postal_code = "N/A",
                city = "N/A",
                country = "EG",
                state = "N/A"
            },
            currency = "EGP",
            integration_id = int.Parse(IntegrationId)
        };

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/acceptance/payment_keys",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString();
    }

    private bool VerifyHmac(PaymentWebhookDto dto, string receivedHmac)
    {
        var hmacSecret = _config["Paymob:HmacSecret"]!;

        // Paymob HMAC string format
        var dataString =
            $"{dto.Obj.Amount_Cents}" +
            $"{dto.Obj.Is_Refund.ToString().ToLower()}" +
            $"{dto.Obj.Order}" +
            $"{dto.Obj.Success.ToString().ToLower()}" +
            $"{dto.Type}";

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hmacSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataString));
        var computedHmac = Convert.ToHexString(hash).ToLower();

        return computedHmac == receivedHmac;
    }
}