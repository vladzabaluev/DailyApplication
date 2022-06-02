namespace DailyApplication.Models
{
    public class UserGroup
    {
        public int Id { get; set; }

        public User User { get; set; }

        public Group Group { get; set; }

        public bool UserIsInGroup { get; set; }

        public UserGroup()
        {
        }
    }
}