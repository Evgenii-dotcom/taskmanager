using Npgsql;
using WpfApp1.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace WpfApp1.Repositories
{
    public class EmployeeRepository
    {
        public Employee GetByLogin(string login)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = "SELECT id, login, password_hash, full_name, role, created_at, is_active " +
                          "FROM employees WHERE login = @login AND is_active = true";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@login", login);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Employee
                            {
                                Id = reader.GetInt32(0),
                                Login = reader.GetString(1),
                                PasswordHash = reader.GetString(2),
                                FullName = reader.GetString(3),
                                Role = reader.GetString(4),
                                CreatedAt = reader.GetDateTime(5),
                                IsActive = reader.GetBoolean(6)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public bool DeleteEmployee(int id)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = "UPDATE employees SET is_active = false WHERE id = @id";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int affectedRows = command.ExecuteNonQuery();
                    return affectedRows > 0;
                }
            }
        }


        public List<Employee> GetAllActiveEmployees()
        {
            var employees = new List<Employee>();

            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = "SELECT id, login, password_hash, full_name, role, created_at, is_active " +
                          "FROM employees WHERE is_active = true ORDER BY full_name";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new Employee
                            {
                                Id = reader.GetInt32(0),
                                Login = reader.GetString(1),
                                PasswordHash = reader.GetString(2),
                                FullName = reader.GetString(3),
                                Role = reader.GetString(4),
                                CreatedAt = reader.GetDateTime(5),
                                IsActive = reader.GetBoolean(6)
                            });
                        }
                    }
                }
            }

            return employees;
        }

        public bool CreateEmployee(Employee employee, string password)
        {
            using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                var sql = "INSERT INTO employees (login, password_hash, full_name, role) " +
                          "VALUES (@login, @password_hash, @full_name, @role)";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@login", employee.Login);
                    command.Parameters.AddWithValue("@password_hash", HashPassword(password));
                    command.Parameters.AddWithValue("@full_name", employee.FullName);
                    command.Parameters.AddWithValue("@role", employee.Role);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}