using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Models;
using WpfApp1.Repositories;

namespace WpfApp1
{
    public partial class ReportsWindow : Window
    {
        private TaskRepository _taskRepository;
        private List<Button> _reportButtons;

        public ReportsWindow()
        {
            InitializeComponent();

            _taskRepository = new TaskRepository();
            LoadCurrentUser();

            // Список всех кнопок отчетов
            _reportButtons = new List<Button>
            {
                AllTasksReportButton,
                ActiveTasksReportButton,
                CompletedTasksReportButton,
                OverdueTasksReportButton,
                TasksByEmployeeReportButton,
                EmployeeStatsReportButton
            };
        }

        private void LoadCurrentUser()
        {
            var currentUser = App.CurrentUser;
            if (currentUser != null)
            {
                CurrentUserNameText.Text = currentUser.FullName;
                CurrentUserRoleText.Text = currentUser.DisplayRole;
            }
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Сброс стилей всех кнопок
                foreach (var b in _reportButtons)
                    b.Style = (Style)FindResource("ReportButtonStyle");

                // Выделяем выбранную
                btn.Style = (Style)FindResource("SelectedReportButtonStyle");

                // Показываем/скрываем панель выбора периода
                if (btn == EmployeeStatsReportButton)
                    PeriodSelectionPanel.Visibility = Visibility.Visible;
                else
                    PeriodSelectionPanel.Visibility = Visibility.Collapsed;

                // Загружаем отчёт
                LoadReport(btn);
            }
        }

        private void LoadReport(Button btn)
        {
            ReportContentPanel.Children.Clear();

            List<Task> tasks = null;

            if (btn == AllTasksReportButton)
            {
                tasks = _taskRepository.GetTasksByEmployee(App.CurrentUser.Id);
            }
            else if (btn == ActiveTasksReportButton)
            {
                tasks = _taskRepository.GetTasksByEmployee(App.CurrentUser.Id)
                    .Where(t => t.Status == "not_accepted" || t.Status == "in_progress").ToList();
            }
            else if (btn == CompletedTasksReportButton)
            {
                tasks = _taskRepository.GetTasksByStatus("completed");
            }
            else if (btn == OverdueTasksReportButton)
            {
                tasks = _taskRepository.GetTasksByStatus("overdue");
            }
            else if (btn == TasksByEmployeeReportButton)
            {
                tasks = _taskRepository.GetAllTasks();
            }
            else if (btn == EmployeeStatsReportButton)
            {
                // По умолчанию показываем статистику за последний месяц
                var start = DateTime.Now.AddMonths(-1);
                var end = DateTime.Now;
                LoadEmployeeStatsReport(start, end);
                return;
            }

            if (tasks == null || !tasks.Any())
            {
                ReportContentPanel.Children.Add(new TextBlock
                {
                    Text = "Нет данных для отображения",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
                return;
            }

            // Заголовок
            var header = new TextBlock
            {
                Text = "Отчёт по задачам",
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            ReportContentPanel.Children.Add(header);

            // Заголовки таблицы
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
            headerRow.Children.Add(CreateCell("№ задачи", 120));
            headerRow.Children.Add(CreateCell("Название", 200));
            headerRow.Children.Add(CreateCell("Статус", 150));
            headerRow.Children.Add(CreateCell("Исполнитель", 150));
            headerRow.Children.Add(CreateCell("Дата создания", 120));
            headerRow.Children.Add(CreateCell("Крайний срок", 120));
            ReportContentPanel.Children.Add(headerRow);

            // Строки задач
            foreach (var task in tasks)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                row.Children.Add(CreateCell(task.TaskNumber, 120));
                row.Children.Add(CreateCell(task.Title, 200));
                row.Children.Add(CreateCell(task.DisplayStatus, 150));
                row.Children.Add(CreateCell(task.AssignedEmployee?.FullName ?? "Не назначен", 150));
                row.Children.Add(CreateCell(task.CreatedAt.ToString("dd.MM.yyyy"), 120));
                row.Children.Add(CreateCell(task.DeadlineDate.ToString("dd.MM.yyyy"), 120));

                ReportContentPanel.Children.Add(row);
            }
        }

        private void LoadEmployeeStatsReport(DateTime start, DateTime end)
        {
            var tasks = _taskRepository.GetTasksByPeriod(start, end);

            var grouped = tasks.GroupBy(t => t.AssignedEmployee?.FullName ?? "Не назначен")
                               .Select(g => new
                               {
                                   Employee = g.Key,
                                   Completed = g.Count(t => t.Status == "completed"),
                                   Overdue = g.Count(t => t.Status == "overdue"),
                                   InProgress = g.Count(t => t.Status == "in_progress" || t.Status == "not_accepted")
                               })
                               .ToList();

            ReportContentPanel.Children.Clear();

            var header = new TextBlock
            {
                Text = $"Статистика сотрудников с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}",
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            ReportContentPanel.Children.Add(header);

            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
            headerRow.Children.Add(CreateCell("Сотрудник", 200));
            headerRow.Children.Add(CreateCell("Выполнено", 100));
            headerRow.Children.Add(CreateCell("Просрочено", 100));
            headerRow.Children.Add(CreateCell("В работе", 100));
            ReportContentPanel.Children.Add(headerRow);

            foreach (var g in grouped)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                row.Children.Add(CreateCell(g.Employee, 200));
                row.Children.Add(CreateCell(g.Completed.ToString(), 100));
                row.Children.Add(CreateCell(g.Overdue.ToString(), 100));
                row.Children.Add(CreateCell(g.InProgress.ToString(), 100));
                ReportContentPanel.Children.Add(row);
            }
        }

        private void ApplyPeriodButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите обе даты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var start = StartDatePicker.SelectedDate.Value;
            var end = EndDatePicker.SelectedDate.Value;

            if (start > end)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadEmployeeStatsReport(start, end);
        }

        private TextBlock CreateCell(string text, double width)
        {
            return new TextBlock
            {
                Text = text,
                Width = width,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 0, 2, 0)
            };
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var main = new MainDashboardWindow();
            main.Show();
            Close();
        }
    }
}
