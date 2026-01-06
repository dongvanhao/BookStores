using BookStore.Application.Dtos.Pricing_Inventory.Discount;
using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Application.Mappers.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Pricing_Inventory
{
    public class DiscountService : IDiscountService
    {
        private readonly IUnitOfWork _uow;

        public DiscountService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<DiscountDto>>> GetActiveAsync()
        {
            var discounts = await _uow.Discount.GetActiveAsync();
            return BaseResult<IReadOnlyList<DiscountDto>>.Ok(
                discounts.Select(d => d.ToDto()).ToList()
            );
        }

        public async Task<BaseResult<Guid>> CreateAsync(CreateDiscountDto dto)
        {
            var discount = new Discount
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                Percentage = dto.Percentage,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            await _uow.Discount.AddAsync(discount);
            await _uow.SaveChangesAsync();

            return BaseResult<Guid>.Ok(discount.Id);
        }

        public async Task<BaseResult<bool>> ToggleAsync(Guid id)
        {
            var discount = await _uow.Discount.GetByIdAsync(id);
            if (discount == null)
                return BaseResult<bool>.NotFound();

            discount.IsActive = !discount.IsActive;
            _uow.Discount.Update(discount);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }
    }
}
