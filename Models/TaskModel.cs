using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class TaskModel
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime DueDate { get; set; } = DateTime.Now;

        [Required]
        public string Priority { get; set; } = "Low";

        [Required]
        public string Status { get; set; } = "To Do";
    }
}
