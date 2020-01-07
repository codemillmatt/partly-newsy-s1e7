using System;
namespace PartlyNewsy.Models
{
    public class UserInterests
    {
        public string NewsCategoryName { get; set; }

        public DateTime DateAdded => DateTime.UtcNow;
    }
}
