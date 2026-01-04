using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.IdentityDto.UserDto
{
    public class UploadAvatarDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
