using System;
using System.Collections.Generic;
using System.Text;

namespace UserClientAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string JwtToken { get; set; }
        public DateTime JwtExpiresAt { get; set; }
        public DateTime RefExpiresAt { get; set; }
        public string RefreshToken { get; set; }
        public string EmployeeId { get; set; }
        public string Country { get; set; }
    }
}
