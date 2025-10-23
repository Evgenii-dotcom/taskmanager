using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1
{
    public class ExecutorActionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status && App.CurrentUser?.Role == "executor")
            {
                // Кнопка "Принять" показывается только исполнителям для новых задач
                return status == "new_task" ? Visibility.Visible : Visibility.Collapsed;
            }

            // Для администратора и директора кнопка "Принять" всегда скрыта
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}