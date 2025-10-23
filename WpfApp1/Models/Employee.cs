using System;

namespace WpfApp1.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public string DisplayRole
        {
            get
            {
                switch (Role)
                {
                    case "admin":
                        return "Администратор";
                    case "director":
                        return "Директор";
                    default:
                        return "Исполнитель";
                }
            }
        }

    }
}