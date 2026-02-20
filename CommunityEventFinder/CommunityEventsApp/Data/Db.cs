using System.Configuration;
using System.Data.SqlClient;

namespace CommunityEventsApp.Data
{
    public static class Db
    {
        public static SqlConnection Open()
        {
            var cs = System.Configuration.ConfigurationManager.ConnectionStrings["AppDb"].ConnectionString;
            var conn = new SqlConnection(cs);
            conn.Open();
            return conn;
        }
    }
}
