using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CommunityEventsApp.Data
{
    public static class Db
    {
        private static string _cs;

        public static void Init(IConfiguration config)
        {
            _cs = config.GetConnectionString("DefaultConnection");
        }

        public static SqlConnection Open()
        {
            var conn = new SqlConnection(_cs);
            conn.Open();
            return conn;
        }
    }
}