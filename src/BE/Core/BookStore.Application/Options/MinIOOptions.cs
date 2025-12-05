using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Options
{
    public class MinIOOptions
    {
        public string Endpoint { get; set;} = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string BucketName { get; set; } = "";
        public bool UseSSL { get; set; } = false; 
    }
}
