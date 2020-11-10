using System;
using System.Collections.Generic;
using System.Text;

namespace UserClientAPI.Models
{
    class UserJwtToken
    {
        public string UserName { get; set; }
        public string JwtToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; }
    }
}
