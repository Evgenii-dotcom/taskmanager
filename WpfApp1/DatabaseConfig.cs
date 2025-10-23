using System.Configuration;

namespace WpfApp1
{
    public static class DatabaseConfig
    {
        public static string ConnectionString { get; } =
            "Host=localhost;Port=5432;Database=taskmanager;Username=postgres;Password=1111";
    }
}