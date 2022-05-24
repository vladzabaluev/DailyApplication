using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DailyApplication.Models
{
    public class Event
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IdentityUser User { get; set; }

        public DateTime DeadlineTime { get; set; }

        public bool IsDone { get; set; }

        public Event()
        {
        }
    }
}