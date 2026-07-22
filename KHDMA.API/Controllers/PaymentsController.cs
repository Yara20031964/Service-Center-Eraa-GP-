using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.Interfaces.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KHDMA.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
     [Authorize]  // Uncomment when authentication is ready
    public class PaymentsController : ControllerBase
    {
        private readonly IStripePaymentService _stripeService;

        public PaymentsController(IStripePaymentService stripeService)
        {
            _stripeService = stripeService;
        }

        [HttpPost("intent/{bookingId}")]
        public async Task<IActionResult> CreatePaymentIntent(Guid bookingId)
        {
            var response = await _stripeService.CreatePaymentIntentAsync(bookingId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// يُستدعى من الـ Frontend بعد نجاح الدفع على Stripe.
        /// يتحقق من صحة الدفع ويُحدّث حالة الحجز تلقائياً.
        /// </summary>
        [HttpPost("confirm/{paymentIntentId}")]
        public async Task<IActionResult> ConfirmPayment(string paymentIntentId)
        {
            var response = await _stripeService.ConfirmPaymentAsync(paymentIntentId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}
