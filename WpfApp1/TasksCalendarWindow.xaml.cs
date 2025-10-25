using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp1.Models;
using WpfApp1.Repositories;

namespace WpfApp1
{
    public partial class TasksCalendarWindow : Window
    {
        private TaskRepository _taskRepository;
        private List<Task> _allTasks;

        public TasksCalendarWindow()
        {
            InitializeComponent();
            _taskRepository = new TaskRepository();

            LoadCurrentUser();
            LoadTasks();
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

        private void LoadTasks()
        {
            var currentUser = App.CurrentUser;
            if (currentUser.Role == "admin" || currentUser.Role == "director" || currentUser.Role == "manager")
            {
                _allTasks = _taskRepository.GetAllTasks(); // Нужно реализовать метод в TaskRepository
            }
            else
            {
                _allTasks = _taskRepository.GetTasksByEmployee(currentUser.Id);
            }

            DrawCalendar();
        }

        private void DrawCalendar()
        {
            CalendarCanvas.Children.Clear();

            if (_allTasks == null || !_allTasks.Any())
                return;

            // сортировка по крайнему сроку
            var tasks = _allTasks.OrderBy(t => t.DeadlineDate).ToList();

            double top = 20;
            double taskHeight = 25;
            double dayWidth = 30;

            var minDate = tasks.Min(t => t.CreatedAt.Date);
            var maxDate = tasks.Max(t => t.DeadlineDate.Date);

            int totalDays = (maxDate - minDate).Days + 1;

            // Подписи дат сверху
            double topOffset = 40; // пространство сверху для дат

            for (int i = 0; i < totalDays; i++)
            {
                var date = minDate.AddDays(i);
                var text = new TextBlock
                {
                    Text = date.ToString("dd.MM"),
                    FontSize = 11,
                    RenderTransform = new RotateTransform(-45, 0, 0), // точка вращения в верхнем левом углу
                    Width = dayWidth + 10, // немного шире, чтобы текст не обрезался
                    TextAlignment = TextAlignment.Left
                };

                Canvas.SetLeft(text, i * dayWidth);
                Canvas.SetTop(text, topOffset); // смещаем вниз
                CalendarCanvas.Children.Add(text);
            }

            // под задачи поднимаем верхний отступ
            top = topOffset + 40;

            // Рисуем задачи
            foreach (var task in tasks)
            {
                int startOffset = (task.CreatedAt.Date - minDate).Days;
                int endOffset = (task.DeadlineDate.Date - minDate).Days;
                double left = startOffset * dayWidth;
                double width = (endOffset - startOffset + 1) * dayWidth;

                var rect = new Border
                {
                    Width = width,
                    Height = taskHeight,
                    CornerRadius = new CornerRadius(4),
                    Background = GetTaskColor(task.Status),
                    ToolTip = $"{task.TaskNumber} ({task.Status})",
                    Tag = task
                };

                rect.MouseLeftButtonDown += TaskRect_MouseLeftButtonDown;

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);

                CalendarCanvas.Children.Add(rect);

                // подпись задачи слева
                var label = new TextBlock
                {
                    Text = task.TaskNumber,
                    FontSize = 12
                };
                // изменяем на:
                Canvas.SetLeft(label, 0); // прямо слева от задачи
                Canvas.SetTop(label, top);
                label.TextAlignment = TextAlignment.Right;
                label.Width = 70; // ширина подписи
                Canvas.SetTop(label, top);
                CalendarCanvas.Children.Add(label);

                top += taskHeight + 10; // следующий уровень "лесенка"
            }

            // Увеличиваем размер Canvas
            CalendarCanvas.Width = totalDays * dayWidth + 100;
            CalendarCanvas.Height = top + 50;
        }

        private Brush GetTaskColor(string status)
        {
            switch (status)
            {
                case "accepted": // Выполнена
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45A866")); // Зеленый
                case "in_progress": // В работе
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#65558F")); // Голубой
                case "overdue": // Просрочена
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000")); // Красный
                case "completed": // Отправлена
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ce2fc")); // Голубой
                default:
                    return Brushes.Gray;
            }
        }


        private void TaskRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Task task)
            {
                SelectedTaskTitle.Text = task.Title;
                DescriptionTextBox.Text = task.Description ?? "";
                AssignedEmployeeText.Text = task.AssignedEmployee?.FullName ?? "Не назначен";
                CreatedDateText.Text = task.CreatedAt.ToString("dd.MM.yyyy");
                DeadlineDateText.Text = task.DeadlineDate.ToString("dd.MM.yyyy");

                // Если есть TextBlock для статуса
                if (this.FindName("StatusText") is TextBlock statusText)
                {
                    statusText.Text = task.Status;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var main = new MainDashboardWindow();
            main.Show();
            Close();
        }
    }
}
