using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Models;
using WpfApp1.Repositories;

namespace WpfApp1
{
    public partial class AddTaskWindow : Window
    {
        private EmployeeRepository _employeeRepository;
        private TaskRepository _taskRepository;
        private string _selectedCategory;

        public AddTaskWindow()
        {
            InitializeComponent();
            _employeeRepository = new EmployeeRepository();
            _taskRepository = new TaskRepository();
            LoadCurrentUser();

            // Генерация номера задачи
            TaskNumberText.Text = GenerateTaskNumber();

            // Устанавливаем дату по умолчанию
            DeadlineDatePicker.SelectedDate = DateTime.Now.AddDays(7);
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

        // Обработчик выбора категории
        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                ResetCategoryButtons();
                button.Style = (Style)FindResource("SelectedCategoryButtonStyle");
                _selectedCategory = button.Content.ToString();
            }
        }

        // Сброс стилей кнопок категорий
        private void ResetCategoryButtons()
        {
            DevelopmentButton.Style = (Style)FindResource("CategoryButtonStyle");
            DesignButton.Style = (Style)FindResource("CategoryButtonStyle");
            TestingButton.Style = (Style)FindResource("CategoryButtonStyle");
            MarketingButton.Style = (Style)FindResource("CategoryButtonStyle");
        }

        // Блокировка ввода текста в DatePicker
        private void DatePicker_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // Подгрузка сотрудников в ComboBox
        private void EmployeeComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var employees = _employeeRepository.GetAllActiveEmployees();
                EmployeeComboBox.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Создание задачи
        private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEmployee = EmployeeComboBox.SelectedItem as Employee;

                // Валидация данных
                if (selectedEmployee == null)
                {
                    MessageBox.Show("Выберите сотрудника", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(_selectedCategory))
                {
                    MessageBox.Show("Выберите категорию задачи", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DeadlineDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите срок сдачи", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DeadlineDatePicker.SelectedDate < DateTime.Today)
                {
                    MessageBox.Show("Срок сдачи не может быть в прошлом", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем задачу
                var task = new Task
                {
                    Title = $"Задача {_selectedCategory}",
                    Description = DescriptionTextBox.Text,
                    TaskNumber = TaskNumberText.Text,
                    Category = _selectedCategory,
                    AssignedTo = selectedEmployee.Id,
                    CreatedBy = App.CurrentUser.Id,
                    DeadlineDate = DeadlineDatePicker.SelectedDate.Value
                };

                bool success = _taskRepository.CreateTask(task);

                if (success)
                {
                    MessageBox.Show("Задача успешно создана!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очистка формы и генерация нового номера
                    ClearForm();
                    TaskNumberText.Text = GenerateTaskNumber();
                }
                else
                {
                    MessageBox.Show("Ошибка при создании задачи", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания задачи: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Генерация номера задачи
        private string GenerateTaskNumber()
        {
            Random rnd = new Random();
            return rnd.Next(1000000, 9999999).ToString();
        }

        // Очистка формы
        private void ClearForm()
        {
            EmployeeComboBox.SelectedItem = null;
            DescriptionTextBox.Text = "";
            DeadlineDatePicker.SelectedDate = DateTime.Now.AddDays(7);
            ResetCategoryButtons();
            _selectedCategory = null;
        }

        // Кнопка отмены
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BackButton_Click(sender, e);
        }

        // Кнопка назад
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainDashboardWindow mainWindow = new MainDashboardWindow();
            mainWindow.Show();
            this.Close();
        }

        // Закрытие приложения
        private void CloseAppButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Обработчик выбора сотрудника (для примера)
        private void EmployeeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedEmployee = EmployeeComboBox.SelectedItem as Employee;
            if (selectedEmployee != null)
            {
                Console.WriteLine($"Выбран сотрудник: {selectedEmployee.FullName}");
            }
        }
    }
}
