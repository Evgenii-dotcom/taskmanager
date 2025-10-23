using Npgsql;
using WpfApp1.Models;
using System;
using System.Collections.Generic;

namespace WpfApp1.Repositories
{
    public class TaskRepository
    {
        public List<Task> GetTasksByEmployee(int employeeId)
        {
            var tasks = new List<Task>();

            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = @"SELECT t.id, t.title, t.description, t.task_number, t.category, 
                                   t.status, t.assigned_to, t.created_by, t.deadline_date,
                                   t.created_at, t.updated_at,
                                   e.full_name as assigned_name
                            FROM tasks t
                            LEFT JOIN employees e ON t.assigned_to = e.id
                            WHERE t.assigned_to = @employeeId
                            ORDER BY t.deadline_date";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@employeeId", employeeId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new Task
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TaskNumber = reader.GetString(3),
                                Category = reader.GetString(4),
                                Status = reader.GetString(5),
                                AssignedTo = reader.GetInt32(6),
                                CreatedBy = reader.GetInt32(7),
                                DeadlineDate = reader.GetDateTime(8),
                                CreatedAt = reader.GetDateTime(9),
                                UpdatedAt = reader.GetDateTime(10),
                                AssignedEmployee = new Employee { FullName = reader.GetString(11) }
                            });
                        }
                    }
                }
            }

            return tasks;
        }
        public bool UpdateTaskDescription(int taskId, string newDescription)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var sql = "UPDATE tasks SET description = @desc WHERE id = @id";
                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@desc", newDescription);
                    cmd.Parameters.AddWithValue("@id", taskId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public List<Task> GetTasksByStatus(string status)
        {
            var tasks = new List<Task>();
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var sql = @"SELECT t.id, t.title, t.description, t.task_number, t.category, t.status, 
                           t.assigned_to, t.created_by, t.deadline_date,
                           t.created_at, t.updated_at,
                           e.full_name as assigned_name
                    FROM tasks t
                    LEFT JOIN employees e ON t.assigned_to = e.id
                    WHERE t.status = @status
                    ORDER BY t.updated_at DESC";
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@status", status);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new Task
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TaskNumber = reader.GetString(3),
                                Category = reader.GetString(4),
                                Status = reader.GetString(5),
                                AssignedTo = reader.GetInt32(6),
                                CreatedBy = reader.GetInt32(7),
                                DeadlineDate = reader.GetDateTime(8),
                                CreatedAt = reader.GetDateTime(9),
                                UpdatedAt = reader.GetDateTime(10),
                                AssignedEmployee = new Employee { FullName = reader.IsDBNull(11) ? "Не назначен" : reader.GetString(11) }
                            });
                        }
                    }
                }
            }
            return tasks;
        }
        public List<Task> GetTasksByPeriod(DateTime start, DateTime end)
        {
            var tasks = new List<Task>();

            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var sql = @"SELECT t.id, t.title, t.description, t.task_number, t.category, t.status, 
                           t.assigned_to, t.created_by, t.deadline_date, t.created_at, t.updated_at,
                           e.full_name as assigned_name
                    FROM tasks t
                    LEFT JOIN employees e ON t.assigned_to = e.id
                    WHERE t.updated_at BETWEEN @start AND @end
                    ORDER BY t.assigned_to, t.updated_at";

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new Task
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TaskNumber = reader.GetString(3),
                                Category = reader.GetString(4),
                                Status = reader.GetString(5),
                                AssignedTo = reader.GetInt32(6),
                                CreatedBy = reader.GetInt32(7),
                                DeadlineDate = reader.GetDateTime(8),
                                CreatedAt = reader.GetDateTime(9),
                                UpdatedAt = reader.GetDateTime(10),
                                AssignedEmployee = new Employee { FullName = reader.IsDBNull(11) ? "Не назначен" : reader.GetString(11) }
                            });
                        }
                    }
                }
            }

            return tasks;
        }
        public bool CreateTask(Task task)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = @"INSERT INTO tasks (title, description, task_number, category, status, 
                                              assigned_to, created_by, deadline_date) 
                           VALUES (@title, @description, @task_number, @category, @status, 
                                   @assigned_to, @created_by, @deadline_date)";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@title", task.Title);
                    command.Parameters.AddWithValue("@description", task.Description ?? "");
                    command.Parameters.AddWithValue("@task_number", task.TaskNumber);
                    command.Parameters.AddWithValue("@category", task.Category);
                    command.Parameters.AddWithValue("@status", "not_accepted");
                    command.Parameters.AddWithValue("@assigned_to", task.AssignedTo);
                    command.Parameters.AddWithValue("@created_by", task.CreatedBy);
                    command.Parameters.AddWithValue("@deadline_date", task.DeadlineDate);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
        public bool AcceptTask(int taskId)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = "UPDATE tasks SET status = 'accepted', updated_at = CURRENT_TIMESTAMP WHERE id = @id";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", taskId);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }


        public List<Task> GetAllCompletedTasks()
        {
            var tasks = new List<Task>();

            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var sql = @"SELECT t.id, t.title, t.description, t.task_number, t.category, 
                           t.status, t.assigned_to, t.created_by, t.deadline_date,
                           t.created_at, t.updated_at,
                           e.full_name as assigned_name,
                           creator.full_name as creator_name
                    FROM tasks t
                    LEFT JOIN employees e ON t.assigned_to = e.id
                    LEFT JOIN employees creator ON t.created_by = creator.id
                    WHERE t.status = 'completed'
                    ORDER BY t.updated_at DESC";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var task = new Task
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TaskNumber = reader.GetString(3),
                                Category = reader.GetString(4),
                                Status = reader.GetString(5),
                                AssignedTo = reader.GetInt32(6),
                                CreatedBy = reader.GetInt32(7),
                                DeadlineDate = reader.GetDateTime(8),
                                CreatedAt = reader.GetDateTime(9),
                                UpdatedAt = reader.GetDateTime(10)
                            };

                            if (!reader.IsDBNull(11))
                            {
                                task.AssignedEmployee = new Employee
                                {
                                    Id = reader.GetInt32(6),
                                    FullName = reader.GetString(11)
                                };
                            }

                            if (!reader.IsDBNull(12))
                            {
                                task.Creator = new Employee
                                {
                                    FullName = reader.GetString(12)
                                };
                            }

                            tasks.Add(task);
                        }
                    }
                }
            }

            return tasks;
        }

        public List<Task> GetAllTasks()
        {
            var tasks = new List<Task>();

            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = @"SELECT t.id, t.title, t.description, t.task_number, t.category, 
                           t.status, t.assigned_to, t.created_by, t.deadline_date,
                           t.created_at, t.updated_at,
                           e.full_name as assigned_name
                    FROM tasks t
                    LEFT JOIN employees e ON t.assigned_to = e.id
                    ORDER BY t.deadline_date";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new Task
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TaskNumber = reader.GetString(3),
                                Category = reader.GetString(4),
                                Status = reader.GetString(5),
                                AssignedTo = reader.GetInt32(6),
                                CreatedBy = reader.GetInt32(7),
                                DeadlineDate = reader.GetDateTime(8),
                                CreatedAt = reader.GetDateTime(9),
                                UpdatedAt = reader.GetDateTime(10),
                                AssignedEmployee = new Employee { FullName = reader.IsDBNull(11) ? "Не назначен" : reader.GetString(11) }
                            });
                        }
                    }
                }
            }

            return tasks;
        }

        public bool UpdateTaskStatus(int taskId, string status)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = "UPDATE tasks SET status = @status, updated_at = CURRENT_TIMESTAMP WHERE id = @id";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@status", status);
                    command.Parameters.AddWithValue("@id", taskId);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}