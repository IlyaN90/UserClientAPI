using System;
using System.Collections.Generic;
using System.Text;

namespace UserClientAPI.Models
{
    public class RegisterUser
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Country { get; set; }

        public string Role { get; set; }

    }
}
