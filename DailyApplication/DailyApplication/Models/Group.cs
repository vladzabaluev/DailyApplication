using System.Collections.Generic;

namespace DailyApplication.Models
{
    public class Group
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<UserGroup> UserGroups { get; set; }

        public List<Event> Events { get; set; }
    }
}
