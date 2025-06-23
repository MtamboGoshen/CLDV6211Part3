using System.ComponentModel.DataAnnotations;

namespace EventEasePOE.Models
{
    public class EventType
    {
        public int EventTypeId { get; set; }

        [Required]
        [Display(Name = "Type Name")]
        public string Name { get; set; } = string.Empty;

        // Optional: Navigation property for related Events
        public List<Event>? Events { get; set; }
    }
}
