using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Domain.Entities.Ordering___Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class PaymentMethodMapping
    {
        public static PaymentMethodDto ToDto(this PaymentMethod method)
        {
            return new PaymentMethodDto
            {
                Id = method.Id,
                Name = method.Name,
                Description = method.Description
            };
        }
    }
}
