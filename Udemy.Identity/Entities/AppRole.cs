using Microsoft.AspNetCore.Identity;

namespace Udemy.Identity.Entities
{
    public class AppRole : IdentityRole<int>
    {
        public DateTime CreatedTime { get; set; }
    }
}
