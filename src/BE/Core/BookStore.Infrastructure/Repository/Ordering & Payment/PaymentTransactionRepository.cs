using BookStore.Domain.Entities.Ordering_Payment;
using BookStore.Domain.IRepository.Ordering_Payment;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .Include(x => x.Refunds)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }
    }
}
