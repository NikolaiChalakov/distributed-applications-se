using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkillForgeFrontend.Models
{
    public class CreateUserViewModel
    {
       
        [Required]
        public string Username { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
