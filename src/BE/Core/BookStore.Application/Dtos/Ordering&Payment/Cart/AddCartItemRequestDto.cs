using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Cart
{
    public class AddCartItemRequestDto
    {
        public Guid BookId { get; set; }
        public int Quantity { get; set; }
    }
}
