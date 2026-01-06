using BookStore.Application.Dtos.Pricing_Inventory.Price;
using BookStore.Application.IService.Pricing_Inventory;
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
    public class PriceService : IPriceService
    {
        private readonly IUnitOfWork _uow;

        public PriceService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<PriceResponseDto>> CreateAsync(CreatePriceRequestDto dto)
        {
            // 1️⃣ Hủy giá hiện tại
            var current = await _uow.Price.GetCurrentByBookAsync(dto.BookId);
            if (current != null)
            {
                current.IsCurrent = false;
                current.EffectiveTo = DateTime.UtcNow;
                _uow.Price.Update(current);
            }

            // 2️⃣ Tạo giá mới
            var price = new Price
            {
                Id = Guid.NewGuid(),
                BookId = dto.BookId,
                Amount = dto.Amount,
                Currency = "VND",
                IsCurrent = true,
                EffectiveFrom = dto.EffectiveFrom,
                DiscountId = dto.DiscountId
            };

            await _uow.Price.AddAsync(price);
            await _uow.SaveChangesAsync();

            return BaseResult<PriceResponseDto>.Ok(new PriceResponseDto
            {
                Id = price.Id,
                Amount = price.Amount,
                IsCurrent = true,
                EffectiveFrom = price.EffectiveFrom,
                DiscountId = price.DiscountId
            });
        }

        public async Task<BaseResult<PriceResponseDto>> GetCurrentAsync(Guid bookId)
        {
            var price = await _uow.Price.GetCurrentByBookAsync(bookId);
            if (price == null)
                return BaseResult<PriceResponseDto>.NotFound("Chưa có giá");

            return BaseResult<PriceResponseDto>.Ok(new PriceResponseDto
            {
                Id = price.Id,
                Amount = ApplyDiscount(price),
                IsCurrent = true,
                EffectiveFrom = price.EffectiveFrom,
                DiscountId = price.DiscountId
            });
        }

        public async Task<BaseResult<IReadOnlyList<PriceResponseDto>>> GetHistoryAsync(Guid bookId)
        {
            var list = await _uow.Price.GetHistoryByBookAsync(bookId);

            return BaseResult<IReadOnlyList<PriceResponseDto>>.Ok(
                list.Select(p => new PriceResponseDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    IsCurrent = p.IsCurrent,
                    EffectiveFrom = p.EffectiveFrom,
                    DiscountId = p.DiscountId
                }).ToList()
            );
        }

        private decimal ApplyDiscount(Price price)
        {
            if (price.Discount == null)
                return price.Amount;

            var discount = price.Discount;

            //  Check còn hiệu lực
            if (!discount.IsActive ||
                discount.StartDate > DateTime.UtcNow ||
                discount.EndDate < DateTime.UtcNow)
                return price.Amount;

            //  Tính % giảm
            var discountAmount = price.Amount * discount.Percentage;

            // Giới hạn giảm tối đa (nếu có)
            if (discount.MaxDiscountAmount.HasValue)
            {
                discountAmount = Math.Min(
                    discountAmount,
                    discount.MaxDiscountAmount.Value
                );
            }

            //  Giá cuối
            return Math.Max(0, price.Amount - discountAmount);
        }

    }
}
