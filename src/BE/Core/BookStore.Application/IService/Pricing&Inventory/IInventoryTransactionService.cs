using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Pricing_Inventory
{
    public interface IInventoryTransactionService
    {
        Task<BaseResult<bool>> OutboundAsync(
        Guid bookId,
        int quantity,
        string referenceId,
        string? note = null);

        Task<BaseResult<bool>> InboundAsync(
            Guid bookId,
            int quantity,
            string referenceId,
            string? note = null);
    }
}
