using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DailyApplication.Models
{
    public class Sub_event
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool isDone { get; set; }

        public Sub_event()
        {
        }
    }
}
