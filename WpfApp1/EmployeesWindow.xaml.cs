using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.Models;
using WpfApp1.Repositories;

namespace WpfApp1
{
    public partial class EmployeesWindow : Window
    {
        private EmployeeRepository _employeeRepository;
        private Employee _currentUser;

        public EmployeesWindow()
        {
            try
            {
                InitializeComponent();
                _employeeRepository = new EmployeeRepository();

                // Добавляем обработчики после инициализации
                ExecutorRoleRadio.Checked += RoleRadioButton_Checked;
                ManagerRoleRadio.Checked += RoleRadioButton_Checked;
                AdminRoleRadio.Checked += RoleRadioButton_Checked;
                DirectorRoleRadio.Checked += RoleRadioButton_Checked;

                LoadCurrentUser();
                LoadEmployees();

                // Обновляем стили после загрузки
                UpdateRoleBorders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна сотрудников: {ex.Message}", "Критическая ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }


        private void LoadCurrentUser()
        {
            try
            {
                _currentUser = App.CurrentUser;

                if (_currentUser != null)
                {
                    CurrentUserNameText.Text = _currentUser.FullName;
                    CurrentUserRoleText.Text = _currentUser.DisplayRole;
                }
                else
                {
                    CurrentUserNameText.Text = "Пользователь не найден";
                    CurrentUserRoleText.Text = "Роль не определена";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = _employeeRepository.GetAllActiveEmployees();

                EmployeesStackPanel.Children.Clear();

                var headerText = new TextBlock
                {
                    Text = "Всего сотрудников: " + employees.Count,
                    FontFamily = new FontFamily("supporting-text"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(0, 0, 0, 15),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                EmployeesStackPanel.Children.Add(headerText);

                foreach (var employee in employees)
                {
                    var employeeCard = CreateEmployeeCard(employee);
                    EmployeesStackPanel.Children.Add(employeeCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateEmployeeCard(Employee employee)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 248, 248)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224))
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // для крестика

            // Имя
            var nameText = new TextBlock
            {
                Text = employee.FullName,
                FontFamily = new FontFamily("supporting-text"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(nameText, 0);
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            // Роль
            SolidColorBrush brush;

            switch (employee.Role)
            {
                case "admin":
                    brush = new SolidColorBrush(Color.FromRgb(101, 85, 143)); // фиолетовый
                    break;

                case "director":
                    brush = new SolidColorBrush(Color.FromRgb(30, 136, 229)); // синий
                    break;

                case "manager": 
                    brush = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // оранжевый
                    break;

                default:
                    brush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // зеленый
                    break;
            }


            var roleBadge = new Border
            {
                Background = brush,
                Padding = new Thickness(6, 2, 6, 2),
                CornerRadius = new CornerRadius(10),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 10, 0)
            };

            var roleText = new TextBlock
            {
                Text = employee.DisplayRole,
                FontFamily = new FontFamily("supporting-text"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            roleBadge.Child = roleText;

            Grid.SetRow(roleBadge, 0);
            Grid.SetColumn(roleBadge, 1);
            grid.Children.Add(roleBadge);

            // Кнопка удаления (крестик)
            var deleteButton = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 0, 0)),
                BorderThickness = new Thickness(0),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Удалить сотрудника",
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            deleteButton.Click += (s, e) =>
            {
                DeleteEmployee(employee);
            };

            // Анимация наведения
            deleteButton.MouseEnter += (s, e) =>
            {
                deleteButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            };
            deleteButton.MouseLeave += (s, e) =>
            {
                deleteButton.Foreground = new SolidColorBrush(Color.FromRgb(170, 0, 0));
            };

            Grid.SetRow(deleteButton, 0);
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            // Логин
            var loginText = new TextBlock
            {
                Text = $"Логин: {employee.Login}",
                FontFamily = new FontFamily("supporting-text"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(loginText, 1);
            Grid.SetColumn(loginText, 0);
            Grid.SetColumnSpan(loginText, 3);
            grid.Children.Add(loginText);

            card.Child = grid;
            return card;
        }

        private void DeleteEmployee(Employee employee)
        {
            if (MessageBox.Show($"Удалить сотрудника {employee.FullName}?", "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = _employeeRepository.DeleteEmployee(employee.Id);
                    if (success)
                    {
                        MessageBox.Show("Сотрудник успешно удалён.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadEmployees();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить сотрудника.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Остальные методы остаются без изменений ↓
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

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = LoginTextBox.Text;
                string password = PasswordBox.Password;
                string fullName = FullNameTextBox.Text;
                string role = "executor";
                if (AdminRoleRadio.IsChecked == true)
                    role = "admin";
                else if (DirectorRoleRadio.IsChecked == true)
                    role = "director";
                else if (ManagerRoleRadio.IsChecked == true)
                    role = "manager";


                if (string.IsNullOrWhiteSpace(login) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(fullName))
                {
                    MessageBox.Show("Заполните все обязательные поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var employee = new Employee
                {
                    Login = login.Trim(),
                    FullName = fullName.Trim(),
                    Role = role
                };

                bool success = _employeeRepository.CreateEmployee(employee, password);

                if (success)
                {
                    MessageBox.Show("Сотрудник успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoginTextBox.Text = "";
                    PasswordBox.Password = "";
                    FullNameTextBox.Text = "";
                    ExecutorRoleRadio.IsChecked = true;
                    LoadEmployees();
                }
                else
                {
                    MessageBox.Show("Ошибка при создании сотрудника", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BackButton_Click(sender, e);
        }

        private void RoleRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateRoleBorders();
        }

        private void RoleBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                if (border.Name == "AdminRoleBorder")
                    AdminRoleRadio.IsChecked = true;
                else if (border.Name == "ExecutorRoleBorder")
                    ExecutorRoleRadio.IsChecked = true;
                else if (border.Name == "DirectorRoleBorder")
                    DirectorRoleRadio.IsChecked = true;
                else if (border.Name == "ManagerRoleBorder") 
                    ManagerRoleRadio.IsChecked = true;
            }
            UpdateRoleBorders();
        }

        private void UpdateRoleBorders()
        {
            DirectorRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            DirectorRoleBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            AdminRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            AdminRoleBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            ExecutorRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            ExecutorRoleBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));

            ManagerRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); 
            ManagerRoleBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));  

            if (DirectorRoleRadio.IsChecked == true)
            {
                DirectorRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 136, 229)); // синий
                DirectorRoleBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            else if (AdminRoleRadio.IsChecked == true)
            {
                AdminRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(101, 85, 143));
                AdminRoleBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            else if (ExecutorRoleRadio.IsChecked == true)
            {
                ExecutorRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(101, 85, 143));
                ExecutorRoleBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            else if (ManagerRoleRadio.IsChecked == true) 
            {
                ManagerRoleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(101, 85, 143));
                ManagerRoleBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
        }
    }
}
