using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using WpfApp1.Models;
using WpfApp1.Repositories;
using Npgsql;

namespace WpfApp1
{
    public partial class TasksWindow : Window
    {
        private TaskRepository _taskRepository;
        private EmployeeRepository _employeeRepository;
        private List<Task> _allTasks;
        private string _currentFilter = "all";
        private string _searchText = "";

        public TasksWindow()
        {
            InitializeComponent();
            _taskRepository = new TaskRepository();
            _employeeRepository = new EmployeeRepository();

            LoadCurrentUser();
            SetupForUserRole();

            // Загружаем сотрудников ДО загрузки задач
            if (IsManagerRole()) // Объединенная проверка для admin/director/manager
            {
                LoadEmployeesForFilter();
            }

            LoadTasks();
        }

        private void LoadCurrentUser()
        {
            try
            {
                var currentUser = App.CurrentUser;
                if (currentUser != null)
                {
                    CurrentUserNameText.Text = currentUser.FullName;
                    CurrentUserRoleText.Text = currentUser.DisplayRole;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Объединенная проверка для ролей с правами управления
        private bool IsManagerRole()
        {
            var currentUser = App.CurrentUser;
            return currentUser != null &&
                   (currentUser.Role == "admin" || currentUser.Role == "director" || currentUser.Role == "manager");
        }

        // Проверка является ли пользователь исполнителем
        private bool IsExecutorRole()
        {
            var currentUser = App.CurrentUser;
            return currentUser != null && currentUser.Role == "executor";
        }

        private void SetupForUserRole()
        {
            var currentUser = App.CurrentUser;
            if (currentUser != null)
            {
                if (IsManagerRole())
                {
                    TasksSubtitleText.Text = "Просмотр всех задач сотрудников";
                    EmployeeFilterComboBox.Visibility = Visibility.Visible;
                }
                else
                {
                    TasksSubtitleText.Text = "Просмотр ваших задач";
                    EmployeeFilterComboBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void EmployeeFilterComboBox_DropDownOpened(object sender, EventArgs e)
        {
            Console.WriteLine("ComboBox открыт, элементов: " + EmployeeFilterComboBox.Items.Count);
        }

        private void LoadEmployeesForFilter()
        {
            try
            {
                var employees = _employeeRepository.GetAllActiveEmployees();

                var allEmployees = new List<Employee> { new Employee { Id = 0, FullName = "Все сотрудники" } };
                allEmployees.AddRange(employees);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    EmployeeFilterComboBox.ItemsSource = allEmployees;
                    EmployeeFilterComboBox.SelectedIndex = 0;
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTasks()
        {
            try
            {
                var currentUser = App.CurrentUser;
                if (currentUser == null) return;

                if (IsManagerRole())
                {
                    _allTasks = GetAllTasksForManager();
                }
                else
                {
                    _allTasks = _taskRepository.GetTasksByEmployee(currentUser.Id);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Task> GetAllTasksForManager()
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
                            ORDER BY t.deadline_date";

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

        private void ApplyFilters()
        {
            if (_allTasks == null) return;

            Console.WriteLine($"Всего задач: {_allTasks.Count}, Фильтр: {_currentFilter}, Поиск: {_searchText}");

            var filteredTasks = _allTasks.AsEnumerable();

            // Применяем фильтр по статусу
            switch (_currentFilter)
            {
                case "active":
                    filteredTasks = filteredTasks.Where(t => t.Status == "in_progress" || t.Status == "not_accepted");
                    break;
                case "completed":
                    filteredTasks = filteredTasks.Where(t => t.Status == "completed");
                    break;
                case "accepted":
                    filteredTasks = filteredTasks.Where(t => t.Status == "accepted");
                    break;
                case "overdue":
                    filteredTasks = filteredTasks.Where(t => t.Status == "overdue");
                    break;
                default:
                    break;
            }

            // Применяем поиск
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filteredTasks = filteredTasks.Where(t =>
                    (t.TaskNumber != null && t.TaskNumber.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.Category != null && t.Category.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.Description != null && t.Description.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.Title != null && t.Title.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Применяем фильтр по сотруднику (только для управляющих ролей)
            if (IsManagerRole() &&
                EmployeeFilterComboBox.SelectedItem is Employee selectedEmployee &&
                selectedEmployee.Id != 0)
            {
                filteredTasks = filteredTasks.Where(t => t.AssignedTo == selectedEmployee.Id);
            }

            var statusOrder = new List<string> { "not_accepted", "in_progress", "completed", "accepted", "overdue" };

            filteredTasks = filteredTasks
                .OrderBy(t => statusOrder.IndexOf(t.Status))
                .ThenBy(t => t.DeadlineDate)
                .ToList();

            var filteredList = UpdateTaskStatuses(filteredTasks.ToList());

            // Создаем отображаемые модели
            var displayTasks = filteredList
                .Select((task, index) => new TaskDisplayModel
                {
                    Number = index + 1,
                    TaskNumber = task.TaskNumber ?? "Без номера",
                    Category = task.Category ?? "Без категории",
                    DeadlineDate = task.DeadlineDate,
                    Status = GetStatusDisplayText(task.Status),
                    OriginalTask = task
                })
                .ToList();

            TasksListView.ItemsSource = displayTasks;
        }

        private List<Task> UpdateTaskStatuses(List<Task> tasks)
        {
            var currentDate = DateTime.Today;
            var updatedTasks = new List<Task>();

            foreach (var task in tasks)
            {
                var updatedTask = task;

                // Не обновляем статус для принятых и завершенных задач
                if (updatedTask.Status != "completed" && updatedTask.Status != "accepted")
                {
                    if (updatedTask.DeadlineDate < currentDate)
                    {
                        updatedTask.Status = "overdue";
                        _taskRepository.UpdateTaskStatus(updatedTask.Id, "overdue");
                    }
                    else if (updatedTask.DeadlineDate.Date == currentDate && updatedTask.Status == "not_accepted")
                    {
                        updatedTask.Status = "in_progress";
                        _taskRepository.UpdateTaskStatus(updatedTask.Id, "in_progress");
                    }
                }

                updatedTasks.Add(updatedTask);
            }

            return updatedTasks;
        }

        private string GetStatusDisplayText(string status)
        {
            switch (status)
            {
                case "completed": return "Сдано";
                case "accepted": return "Выполнено";
                case "overdue": return "Просрочка";
                case "in_progress": return "В работе";
                case "not_accepted": return "Не принята";
                default: return status;
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                ResetFilterButtons();
                button.Style = (Style)FindResource("SelectedFilterButtonStyle");

                switch (button.Content.ToString())
                {
                    case "Все задачи": _currentFilter = "all"; break;
                    case "Активные": _currentFilter = "active"; break;
                    case "Сданные": _currentFilter = "completed"; break;
                    case "Выполненные": _currentFilter = "accepted"; break;
                    case "Просроченные": _currentFilter = "overdue"; break;
                    default: _currentFilter = "all"; break;
                }

                ApplyFilters();
            }
        }

        private void ResetFilterButtons()
        {
            AllTasksButton.Style = (Style)FindResource("FilterButtonStyle");
            ActiveTasksButton.Style = (Style)FindResource("FilterButtonStyle");
            CompletedTasksButton.Style = (Style)FindResource("FilterButtonStyle");
            AcceptedTasksButton.Style = (Style)FindResource("FilterButtonStyle");
            OverdueTasksButton.Style = (Style)FindResource("FilterButtonStyle");
        }

        private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                ShowTaskDetails(selectedTask.OriginalTask);
            }
            else
            {
                HideTaskDetails();
            }
        }

        private void ShowTaskDetails(Task task)
        {
            try
            {
                SelectedTaskTitle.Text = $"Задание {task.TaskNumber}";
                DescriptionTextBox.Text = task.Description;

                AssignedEmployeeText.Text = task.AssignedEmployee?.FullName ?? "Не назначен";
                DeadlineDateText.Text = task.DeadlineDate.ToString("dd.MM.yyyy");

                var fileInfo = GetTaskFileInfo(task.Id);

                if (fileInfo != null && File.Exists(fileInfo.FilePath))
                {
                    ReportTextBox.Text = $"Загружен файл: {fileInfo.FileName}\nПуть: {fileInfo.FilePath}";
                    DownloadReportButton.Visibility = Visibility.Visible;
                }
                else
                {
                    ReportTextBox.Text = "Отчет не загружен";
                    DownloadReportButton.Visibility = Visibility.Collapsed;
                }

                TaskDescriptionPanel.Visibility = Visibility.Visible;
                TaskReportPanel.Visibility = Visibility.Visible;

                var currentUser = App.CurrentUser;
                if (currentUser == null) return;

                // СБРОСИМ ВСЕ КНОПКИ ПЕРЕД НАСТРОЙКОЙ
                ResetActionButtons();

                // Настройка для исполнителя
                if (IsExecutorRole() &&
                    task.AssignedTo == currentUser.Id &&
                    (task.Status == "in_progress" || task.Status == "not_accepted" || task.Status == "overdue"))
                {
                    TaskActionsPanel.Visibility = Visibility.Visible;
                    UploadReportButton.Visibility = Visibility.Visible;
                    CompleteTaskButton.Visibility = Visibility.Visible;

                    // Если задача не принята, показываем кнопку "Принять задачу"
                    if (task.Status == "not_accepted")
                    {
                        AcceptRejectedTaskButton.Visibility = Visibility.Visible;
                    }
                }
                // Настройка для управляющих ролей (admin/director/manager)
                else if (IsManagerRole() && task.Status == "completed" && task.CreatedBy == currentUser.Id)
                {
                    TaskActionsPanel.Visibility = Visibility.Visible;
                    ApproveTaskButton.Visibility = Visibility.Visible;
                    RejectTaskButton.Visibility = Visibility.Visible;

                    // Скрываем кнопки исполнителя
                    UploadReportButton.Visibility = Visibility.Collapsed;
                    CompleteTaskButton.Visibility = Visibility.Collapsed;
                    AcceptRejectedTaskButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TaskActionsPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении деталей задачи: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetActionButtons()
        {
            UploadReportButton.Visibility = Visibility.Collapsed;
            CompleteTaskButton.Visibility = Visibility.Collapsed;
            ApproveTaskButton.Visibility = Visibility.Collapsed;
            RejectTaskButton.Visibility = Visibility.Collapsed;
            AcceptRejectedTaskButton.Visibility = Visibility.Collapsed;
        }

        private void ApproveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                bool success = _taskRepository.AcceptTask(selectedTask.OriginalTask.Id);
                if (success)
                {
                    MessageBox.Show("Задача принята и отмечена как выполненная!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadTasks();

                    // Скрываем панель действий после принятия
                    TaskActionsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show("Ошибка при принятии задачи", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RejectTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                var commentWindow = new CommentWindow();
                if (commentWindow.ShowDialog() == true)
                {
                    string comment = commentWindow.CommentText;

                    // Дополняем описание
                    selectedTask.OriginalTask.Description += $"\n\nКомментарий администратора: {comment}";
                    _taskRepository.UpdateTaskDescription(selectedTask.OriginalTask.Id, selectedTask.OriginalTask.Description);

                    // Меняем статус
                    _taskRepository.UpdateTaskStatus(selectedTask.OriginalTask.Id, "not_accepted");

                    MessageBox.Show("Задача отклонена и возвращена исполнителю!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadTasks();
                }
            }
        }

        private void AcceptRejectedTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                bool success = _taskRepository.UpdateTaskStatus(selectedTask.OriginalTask.Id, "in_progress");
                if (success)
                {
                    MessageBox.Show("Задача принята исполнителем и переведена в работу.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadTasks();
                }
            }
        }

        private void HideTaskDetails()
        {
            SelectedTaskTitle.Text = "Выберите задачу для просмотра";
            TaskDescriptionPanel.Visibility = Visibility.Collapsed;
            TaskReportPanel.Visibility = Visibility.Collapsed;
            TaskActionsPanel.Visibility = Visibility.Collapsed;
            DownloadReportButton.Visibility = Visibility.Collapsed;
        }

        private TaskFileInfo GetTaskFileInfo(int taskId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    connection.Open();

                    var sql = @"SELECT id, task_id, file_name, file_path, uploaded_by, uploaded_at 
                       FROM task_files 
                       WHERE task_id = @task_id 
                       ORDER BY uploaded_at DESC 
                       LIMIT 1";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@task_id", taskId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new TaskFileInfo
                                {
                                    Id = reader.GetInt32(0),
                                    TaskId = reader.GetInt32(1),
                                    FileName = reader.GetString(2),
                                    FilePath = reader.GetString(3),
                                    UploadedBy = reader.GetInt32(4),
                                    UploadedAt = reader.GetDateTime(5)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения информации о файле: {ex.Message}");
            }

            return null;
        }

        private void UploadReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                try
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "Все файлы (*.*)|*.*|Документы (*.docx;*.pdf;*.txt)|*.docx;*.pdf;*.txt|Изображения (*.jpg;*.png)|*.jpg;*.png",
                        FilterIndex = 2,
                        Title = "Выберите файл отчета"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        string filePath = openFileDialog.FileName;
                        string fileName = System.IO.Path.GetFileName(filePath);

                        bool success = SaveFileInfo(selectedTask.OriginalTask.Id, fileName, filePath);

                        if (success)
                        {
                            MessageBox.Show($"Файл '{fileName}' успешно загружен!", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);

                            // Обновляем детали задачи
                            ShowTaskDetails(selectedTask.OriginalTask);
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при сохранении информации о файле", "Ошибка",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите задачу для загрузки отчета", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DownloadReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                try
                {
                    var fileInfo = GetTaskFileInfo(selectedTask.OriginalTask.Id);
                    if (fileInfo != null && File.Exists(fileInfo.FilePath))
                    {
                        var saveFileDialog = new SaveFileDialog
                        {
                            FileName = fileInfo.FileName,
                            Filter = "Все файлы (*.*)|*.*",
                            Title = "Сохранить файл отчета"
                        };

                        if (saveFileDialog.ShowDialog() == true)
                        {
                            File.Copy(fileInfo.FilePath, saveFileDialog.FileName, true);
                            MessageBox.Show($"Файл успешно сохранен как: {saveFileDialog.FileName}", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Файл отчета не найден или был перемещен", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при скачивании файла: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool SaveFileInfo(int taskId, string fileName, string filePath)
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    connection.Open();

                    var sql = @"INSERT INTO task_files (task_id, file_name, file_path, uploaded_by) 
                               VALUES (@task_id, @file_name, @file_path, @uploaded_by)";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@task_id", taskId);
                        command.Parameters.AddWithValue("@file_name", fileName);
                        command.Parameters.AddWithValue("@file_path", filePath);
                        command.Parameters.AddWithValue("@uploaded_by", App.CurrentUser.Id);

                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения файла: {ex.Message}");
                return false;
            }
        }

        private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskDisplayModel selectedTask)
            {
                try
                {
                    var fileInfo = GetTaskFileInfo(selectedTask.OriginalTask.Id);
                    if (fileInfo == null)
                    {
                        var result = MessageBox.Show("Отчет не загружен. Вы уверены, что хотите сдать задачу без отчета?",
                                                   "Подтверждение",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    bool success = _taskRepository.UpdateTaskStatus(selectedTask.OriginalTask.Id, "completed");

                    if (success)
                    {
                        LoadTasks();
                        MessageBox.Show("Задача успешно сдана!", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем детали задачи после сдачи
                        if (TasksListView.SelectedItem is TaskDisplayModel updatedTask)
                        {
                            ShowTaskDetails(updatedTask.OriginalTask);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при обновлении статуса задачи", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сдаче задачи: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text;
            ApplyFilters();
        }

        private void EmployeeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainDashboardWindow mainWindow = new MainDashboardWindow();
            mainWindow.Show();
            this.Close();
        }

        private void CloseAppButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class TaskDisplayModel
    {
        public int Number { get; set; }
        public string TaskNumber { get; set; }
        public string Category { get; set; }
        public DateTime DeadlineDate { get; set; }
        public string Status { get; set; }
        public Task OriginalTask { get; set; }
    }

    public class TaskFileInfo
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}