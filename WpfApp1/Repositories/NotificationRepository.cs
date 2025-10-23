using Npgsql;
using System;
using System.Collections.Generic;
using WpfApp1.Models;

namespace WpfApp1.Repositories
{
    public class NotificationRepository
    {
        private readonly TaskRepository _taskRepo = new TaskRepository();

        public List<Notification> GetNotificationsForUser(Employee user)
        {
            var notifications = new List<Notification>();

            // Сначала загружаем сохраненные уведомления из БД
            var savedNotifications = GetSavedNotifications();

            if (user.Role == "admin" || user.Role == "director")
            {
                // Для администратора и директора - завершенные задачи
                var completedTasks = _taskRepo.GetAllCompletedTasks();
                foreach (var task in completedTasks)
                {
                    // Проверяем, нет ли уже уведомления для этой задачи
                    if (!IsNotificationExists(savedNotifications, task.Id, "completed"))
                    {
                        notifications.Add(new Notification
                        {
                            Id = GenerateNotificationId(), // Генерируем временный ID
                            TaskId = task.Id,
                            TaskNumber = task.TaskNumber,
                            Status = "completed",
                            Deadline = task.DeadlineDate,
                            Message = $"Задача {task.TaskNumber} сдана исполнителем {task.AssignedEmployee?.FullName ?? "Неизвестно"}",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            NotificationType = "completed_task"
                        });
                    }
                }
            }
            else if (user.Role == "executor")
            {
                // Для исполнителя - непринятые задачи
                var tasks = _taskRepo.GetTasksByEmployee(user.Id);
                foreach (var task in tasks)
                {
                    if (task.Status == "not_accepted" && !IsNotificationExists(savedNotifications, task.Id, "new_task"))
                    {
                        notifications.Add(new Notification
                        {
                            Id = GenerateNotificationId(), // Генерируем временный ID
                            TaskId = task.Id,
                            TaskNumber = task.TaskNumber,
                            Status = "new_task",
                            Deadline = task.DeadlineDate,
                            Message = $"Новая задача: {task.TaskNumber} - {task.Title}",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            NotificationType = "new_task"
                        });
                    }
                }
            }

            return notifications;
        }

        private List<Notification> GetSavedNotifications()
        {
            var notifications = new List<Notification>();

            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var sql = "SELECT id, task_id, notification_type, is_read, created_at FROM notifications";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notifications.Add(new Notification
                            {
                                Id = reader.GetInt32(0),
                                TaskId = reader.GetInt32(1),
                                NotificationType = reader.GetString(2),
                                IsRead = reader.GetBoolean(3),
                                CreatedAt = reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }

            return notifications;
        }

        private bool IsNotificationExists(List<Notification> savedNotifications, int taskId, string notificationType)
        {
            return savedNotifications.Exists(n => n.TaskId == taskId && n.NotificationType == notificationType && n.IsRead);
        }

        private int GenerateNotificationId()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode()); // Временный ID для UI
        }

        public void MarkNotificationAsRead(int taskId, string notificationType)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                // Сначала проверяем, существует ли уже запись
                var checkSql = "SELECT COUNT(1) FROM notifications WHERE task_id = @taskId AND notification_type = @type";
                bool exists = false;

                using (var checkCommand = new NpgsqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@taskId", taskId);
                    checkCommand.Parameters.AddWithValue("@type", notificationType);
                    var count = (long)checkCommand.ExecuteScalar();
                    exists = count > 0;
                }

                if (exists)
                {
                    // Обновляем существующую запись
                    var updateSql = "UPDATE notifications SET is_read = true WHERE task_id = @taskId AND notification_type = @type";
                    using (var updateCommand = new NpgsqlCommand(updateSql, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@taskId", taskId);
                        updateCommand.Parameters.AddWithValue("@type", notificationType);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Создаем новую запись
                    var insertSql = @"INSERT INTO notifications (task_id, notification_type, is_read, created_at) 
                                     VALUES (@taskId, @type, true, @createdAt)";
                    using (var insertCommand = new NpgsqlCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@taskId", taskId);
                        insertCommand.Parameters.AddWithValue("@type", notificationType);
                        insertCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteNotification(int taskId, string notificationType)
        {
            MarkNotificationAsRead(taskId, notificationType); // Просто помечаем как прочитанное
        }
    }
}