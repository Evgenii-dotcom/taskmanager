using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Repositories;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoginTextBox == null || PasswordBox == null)
                return;

            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                var employeeRepository = new EmployeeRepository();
                var employee = employeeRepository.GetByLogin(login);

                

                if (employee != null)
                {
                   

                    bool passwordValid = VerifyPassword(password, employee.PasswordHash);
                    

                    if (passwordValid)
                    {
                        App.CurrentUser = employee;
                        MainDashboardWindow mainDashboard = new MainDashboardWindow();
                        mainDashboard.Show();
                        this.Close();
                        return;
                    }
                }

                MessageBox.Show("Неверный логин или пароль.", "Ошибка входа",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash) == passwordHash;
            }
        }

        // Обработчик нажатия кнопки закрытия
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }






    }

}
