using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Models;
using WpfApp1.Repositories;

namespace WpfApp1
{
    public partial class MainDashboardWindow : Window
    {
        private NotificationRepository _notificationRepo = new NotificationRepository();
        private List<Notification> _notifications = new List<Notification>();

        public MainDashboardWindow()
        {
            InitializeComponent();
            LoadCurrentUser();
            ConfigureVisibilityForRole();
            LoadNotifications();
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

        private void ConfigureVisibilityForRole()
        {
            var currentUser = App.CurrentUser;
            if (currentUser == null) return;

            if (currentUser.Role == "executor")
            {
                EmployeesButton.Visibility = Visibility.Collapsed;
                // Executor не может добавлять задачи, скрываем кнопку
                AddTasksButton.Visibility = Visibility.Collapsed;
            }

            // Для директора и других ролей кнопка будет видна, ничего не скрываем
        }


        private void LoadNotifications()
        {
            try
            {
                var currentUser = App.CurrentUser;
                if (currentUser == null) return;

                _notifications = _notificationRepo.GetNotificationsForUser(currentUser);
                NotificationsItemsControl.ItemsSource = null; // Сначала очищаем
                NotificationsItemsControl.ItemsSource = _notifications;

                Console.WriteLine($"Загружено уведомлений: {_notifications.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NotificationActionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is Notification notification)
                {
                    if (App.CurrentUser.Role == "executor" && notification.Status == "new_task")
                    {
                        var taskRepo = new TaskRepository();
                        bool success = taskRepo.UpdateTaskStatus(notification.TaskId, "in_progress");

                        if (success)
                        {
                            // Помечаем уведомление как прочитанное
                            _notificationRepo.MarkNotificationAsRead(notification.TaskId, "new_task");

                            // Обновляем список
                            LoadNotifications();

                            MessageBox.Show("Задача принята в работу!", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при принятии задачи: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is Notification notification)
                {
                    Console.WriteLine($"Удаление уведомления: TaskId={notification.TaskId}, Type={notification.NotificationType}");

                    // Помечаем уведомление как прочитанное/удаленное
                    _notificationRepo.MarkNotificationAsRead(notification.TaskId, notification.NotificationType);

                    // Удаляем из локального списка
                    _notifications.RemoveAll(n => n.TaskId == notification.TaskId && n.NotificationType == notification.NotificationType);

                    // Обновляем UI
                    NotificationsItemsControl.ItemsSource = null;
                    NotificationsItemsControl.ItemsSource = _notifications;

                    Console.WriteLine($"Уведомление удалено. Осталось: {_notifications.Count}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении уведомления: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Остальные методы остаются без изменений...
        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            TasksCalendarWindow tasksCalendarWindow = new TasksCalendarWindow();
            tasksCalendarWindow.Show();
            this.Close();
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            EmployeesWindow employeesWindow = new EmployeesWindow();
            employeesWindow.Show();
            this.Close();
        }

        private void MyTasksButton_Click(object sender, RoutedEventArgs e)
        {
            TasksWindow taskWindow = new TasksWindow();
            taskWindow.Show();
            this.Close();
        }

        private void AddTasksButton_Click(object sender, RoutedEventArgs e)
        {
            AddTaskWindow addTaskWindow = new AddTaskWindow();
            addTaskWindow.Show();
            this.Close();
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow();
            reportsWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        private void CloseAppButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}