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
    public class RefundRepository
        : GenericRepository<Refund>, IRefundRepository
    {
        public RefundRepository(AppDbContext context)
            : base(context) { }

        public async Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentTransactionId)
        {
            return await _context.Set<Refund>()
                .Where(x => x.PaymentTransactionId == paymentTransactionId)
                .OrderByDescending(x => x.RequestedAt)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
