using System.ComponentModel.DataAnnotations;

namespace SkillForge.DTO.Users
{
    public class LoginDto
    {
        [Required]
        public string Username{ get; set; }
        [Required] 
        public string Password{ get; set; }
    }
}
