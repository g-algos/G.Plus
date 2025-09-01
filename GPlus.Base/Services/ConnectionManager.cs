using GPlus.Base.Helpers;
using Npgsql;
using System.IO;

namespace GPlus.Base.Services
{
    public class ConnectionManager
    {
        public ConnectionManager()
        {
            Load();
        }

        private string GetKey() => new string("A7d93fGPlusProdKey#2025".Reverse().ToArray());
        private string ConnectionString { get; set; }
        private void Load()
        {
            var lines = File.ReadAllLines("db.config")
                            .Select(line => line.Split(':'))
                            .ToDictionary(x => x[0], x => x[1]);
            var passwordDecrypted = CryptoUtils.Decrypt(lines["password"], GetKey());
            ConnectionString = $"Host={lines["host"]};Port={lines["port"]};Database={lines["database"]};Username={lines["username"]};Password={passwordDecrypted};SslMode=Require;Trust Server Certificate=true;";
        }
        public void LogUserToDatabase(string loginUserId, string userName, string product)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            string sql = @"
        INSERT INTO public.""ActiveUsers""
        (created_at, login_user_id, user_name, product)
        VALUES (NOW(), @login_user_id, @user_name, @product);
    ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login_user_id", loginUserId ?? string.Empty);
            cmd.Parameters.AddWithValue("user_name", userName ?? string.Empty);
            cmd.Parameters.AddWithValue("product", product ?? string.Empty);
            cmd.ExecuteNonQuery();
        }


    }
}
