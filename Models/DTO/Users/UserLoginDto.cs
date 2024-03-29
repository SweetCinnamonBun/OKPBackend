using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OKPBackend.Models.DTO.Users
{
    public class UserLoginDto
    {
        [Required]
        [DataType(DataType.EmailAddress, ErrorMessage = "Please provide a valid email address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}