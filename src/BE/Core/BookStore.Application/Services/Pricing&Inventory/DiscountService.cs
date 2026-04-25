using BookStore.Application.Dtos.Pricing_Inventory.Discount;
using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Application.Mappers.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Pricing___Inventory;

namespace BookStore.Application.Services.Pricing_Inventory
{
    public class DiscountService : IDiscountService
    {
        private readonly IDiscountRepository _discounts;
        private readonly IDbSession _session;

        public DiscountService(IDiscountRepository discounts, IDbSession session)
        {
            _discounts = discounts;
            _session = session;
        }

        public async Task<BaseResult<IReadOnlyList<DiscountDto>>> GetActiveAsync()
        {
            var discounts = await _discounts.GetActiveAsync();
            return BaseResult<IReadOnlyList<DiscountDto>>.Ok(
                discounts.Select(d => d.ToDto()).ToList()
            );
        }

        public async Task<BaseResult<Guid>> CreateAsync(CreateDiscountDto dto)
        {
            var discount = new Discount
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Percentage = dto.Percentage,
                MaxAmount = dto.MaxAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            await _discounts.AddAsync(discount);
            await _session.SaveChangesAsync();

            return BaseResult<Guid>.Ok(discount.Id);
        }

        public async Task<BaseResult<bool>> ToggleAsync(Guid id)
        {
            var discount = await _discounts.GetByIdAsync(id);
            if (discount == null)
                return BaseResult<bool>.NotFound();

            discount.IsActive = !discount.IsActive;
            _discounts.Update(discount);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }
    }
}
