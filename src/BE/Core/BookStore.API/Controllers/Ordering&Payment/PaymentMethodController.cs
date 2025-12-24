using BookStore.Application.IService.Ordering_Payment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Ordering_Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodController : BaseController
    {
        private readonly IPaymentMethodService _service;

        public PaymentMethodController(IPaymentMethodService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            return FromResult(await _service.GetActiveAsync());
        }
    }
}
