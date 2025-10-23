using System;

namespace WpfApp1.Models
{
    public class Notification
    {
        public int Id { get; set; } // Добавляем ID
        public int TaskId { get; set; }
        public string TaskNumber { get; set; }
        public string Status { get; set; }
        public DateTime Deadline { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } // Когда создано уведомление
        public bool IsRead { get; set; } // Прочитано ли
        public string NotificationType { get; set; } // Тип уведомления
    }
}