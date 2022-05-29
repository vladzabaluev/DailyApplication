using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace DailyApplication.Models
{
    public class User : IdentityUser
    {
        public List<UserGroup> UserGroup { get; set; }
    }
}