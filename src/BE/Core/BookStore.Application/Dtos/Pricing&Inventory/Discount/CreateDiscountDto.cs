using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory.Discount
{
    public class CreateDiscountDto
    {
        public string Code { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Percentage { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

}
