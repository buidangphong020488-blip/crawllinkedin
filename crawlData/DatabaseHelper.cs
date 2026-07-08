using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crawlData
{
    public class DatabaseHelper
    {
        // Đường dẫn file database
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
        private static string connectionString = $"Data Source={dbPath}";

        public static void InitializeDatabase()
        {
            // Kiểm tra nếu file đã tồn tại thì không cần làm gì cả (tùy chọn)
            // Tuy nhiên, dùng "IF NOT EXISTS" là cách chắc chắn nhất để bảo vệ cấu trúc bảng
            SQLitePCL.Batteries.Init();

            // Kiểm tra file vật lý
            if (File.Exists(dbPath))
            {
                // Nếu bạn muốn chắc chắn hơn, có thể return luôn tại đây
                // return; 
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                // Bật WAL mode cho đa luồng đọc/ghi đồng thời
                var pragmaCmd = connection.CreateCommand();
                pragmaCmd.CommandText = "PRAGMA journal_mode=WAL;";
                pragmaCmd.ExecuteNonQuery();
                var command = connection.CreateCommand();

                // Sử dụng IF NOT EXISTS cho tất cả các bảng
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Config (
                    aistudio_key TEXT,
                    saleql_key TEXT,
                    proxy TEXT,
                    openai_key TEXT,
                    linkedin_id TEXT,
                    linkedin_pass TEXT,
                    linkedin_cookies TEXT,
                    gemini_model TEXT,
                    linkedin_keywords TEXT
                );

                CREATE TABLE IF NOT EXISTS Company (
                    ID TEXT PRIMARY KEY,
                    CompanyName TEXT,
                    Address TEXT,
                    Website TEXT,
                    Email TEXT,
                    Linkedin TEXT,
                    Phone TEXT,
                    LastUpdate TEXT
                );

                CREATE TABLE IF NOT EXISTS Person (
                    ID TEXT PRIMARY KEY,
                    CompanyID TEXT,
                    FullName TEXT,
                    Position TEXT,
                    Linkedin TEXT,
                    Email TEXT,
                    Phone TEXT,
                    LastUpdate TEXT,
                    FOREIGN KEY (CompanyID) REFERENCES Company(ID)
                );

                CREATE TABLE IF NOT EXISTS CompanyGroup (
                    ID TEXT PRIMARY KEY,
                    GroupName TEXT,
                    CompanyID TEXT,
                    FOREIGN KEY (CompanyID) REFERENCES Company(ID)
                );";

                command.ExecuteNonQuery();

                // Migrate Config for new columns
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN openai_key TEXT"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN linkedin_id TEXT"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN linkedin_pass TEXT"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN linkedin_cookies TEXT"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN captcha_key TEXT"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN gemini_model TEXT"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE Config ADD COLUMN linkedin_keywords TEXT"; command.ExecuteNonQuery(); } catch { }

                // Migrate ID for existing Person table if missing
                try 
                {
                    command.CommandText = "ALTER TABLE Person ADD COLUMN ID TEXT";
                    command.ExecuteNonQuery();
                    
                    // Generate GUIDs for existing rows where ID is null
                    command.CommandText = "UPDATE Person SET ID = lower(hex(randomblob(16))) WHERE ID IS NULL";
                    command.ExecuteNonQuery();
                } 
                catch 
                { 
                    // Column might already exist 
                }
            }
        }
        // Hàm thực thi lệnh INSERT/UPDATE/DELETE (Không trả về dữ liệu)
        public static void ExecuteNonQuery(string sql, SqliteParameter[] parameters = null)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Hàm lấy dữ liệu (Trả về DataTable)
        public static DataTable ExecuteQuery(string sql, SqliteParameter[] parameters = null)
        {
            DataTable dt = new DataTable();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }
        // Hàm thực thi Transaction (Batch Insert/Update) - QUAN TRỌNG ĐỂ TĂNG TỐC ĐỘ insert/update
        public static void ExecuteBatch(Action<SqliteConnection, SqliteTransaction> action)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Gọi action được truyền vào, cung cấp connection và transaction đang mở
                        action(conn, transaction);
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw; // Ném lại lỗi để bên ngoài biết
                    }
                }
            }
        }


    }
}
