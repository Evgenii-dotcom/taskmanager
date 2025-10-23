using System;

namespace WpfApp1.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TaskNumber { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public int AssignedTo { get; set; }
        public int CreatedBy { get; set; }
        public DateTime DeadlineDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Employee AssignedEmployee { get; set; }
        public Employee Creator { get; set; }

        public string DisplayStatus
        {
            get
            {
                switch (Status)
                {
                    case "not_accepted": return "Не принята";
                    case "in_progress": return "В работе";
                    case "completed": return "Сдана";
                    case "accepted": return "Выполнена"; // Новый статус
                    case "overdue": return "Просрочена";
                    default: return Status;
                }
            }
        }
    }
}