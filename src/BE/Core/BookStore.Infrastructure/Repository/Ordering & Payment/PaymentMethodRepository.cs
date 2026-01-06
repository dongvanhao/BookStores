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
    public class PaymentMethodRepository
        : GenericRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public PaymentMethodRepository(AppDbContext context)
            : base(context) { }

        public async Task<IReadOnlyList<PaymentMethod>> GetActiveAsync()
        {
            return await _context.Set<PaymentMethod>()
                .Where(x => x.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
