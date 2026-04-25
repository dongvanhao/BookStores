using BookStore.Domain.Entities.Ordering;
using BookStore.Domain.IRepository.Ordering_Payment;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository.Ordering_Payment
{
    public class PaymentTransactionRepository
        : GenericRepository<PaymentTransaction>, IPaymentTransactionRepository
    {
        public PaymentTransactionRepository(AppDbContext context)
            : base(context) { }

        public async Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.Set<PaymentTransaction>()
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }
    }
}
