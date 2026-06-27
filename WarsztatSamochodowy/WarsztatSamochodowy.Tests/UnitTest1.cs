using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Xunit;

namespace WarsztatSamochodowy.Tests
{
    public class WorkshopDatabaseTests : IDisposable
    {
        private readonly string _databaseName = "TestWarsztatDb_" + Guid.NewGuid().ToString("N");
        private readonly string _scriptPath;
        private readonly WorkshopDatabase _database;

        public WorkshopDatabaseTests()
        {
            _scriptPath = FindSqlScriptPath();
            _database = new WorkshopDatabase(_databaseName);
        }

        private static string FindSqlScriptPath()
        {
            var directory = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(directory))
            {
                var scriptPath = Path.Combine(directory, "Database", "init_database.sql");
                if (File.Exists(scriptPath))
                {
                    return Path.GetFullPath(scriptPath);
                }

                var parent = Path.GetDirectoryName(directory);
                if (string.IsNullOrEmpty(parent) || parent == directory)
                {
                    break;
                }

                directory = parent;
            }

            throw new FileNotFoundException("SQL script not found in any parent directory.");
        }

        [Fact]
        public void EnsureDatabaseInitialized_CreatesDatabaseAndInsertsSampleData()
        {
            Assert.True(File.Exists(_scriptPath), $"SQL script not found: {_scriptPath}");

            _database.EnsureDatabaseInitialized(_scriptPath);

            Assert.True(_database.DatabaseExists());
            Assert.True(_database.ValueExists("SELECT COUNT(*) FROM SL_Podmioty WHERE NIP = @nip", new SqlParameter("@nip", "1112223334")));
            Assert.True(_database.ValueExists("SELECT COUNT(*) FROM T_Pojazdy WHERE VIN LIKE 'VIN%'"));
            Assert.True(_database.ValueExists("SELECT COUNT(*) FROM T_Zlecenia WHERE VIN LIKE 'VIN%'"));
        }

        [Fact]
        public void EnsureDatabaseInitialized_CanBeCalledTwiceWithoutError()
        {
            Assert.True(File.Exists(_scriptPath), $"SQL script not found: {_scriptPath}");

            _database.EnsureDatabaseInitialized(_scriptPath);
            _database.EnsureDatabaseInitialized(_scriptPath);

            Assert.True(_database.ValueExists("SELECT COUNT(*) FROM SL_Podmioty WHERE NIP = @nip", new SqlParameter("@nip", "1112223334")));
            Assert.True(_database.ValueExists("SELECT COUNT(*) FROM T_Pojazdy WHERE VIN LIKE 'VIN%'"));
        }

        private bool TestDatabaseExists()
        {
            using var connection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=master;TrustServerCertificate=true;");
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
            command.Parameters.AddWithValue("@name", _databaseName);
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar() ?? 0) > 0;
        }

        public void Dispose()
        {
            try
            {
                if (!TestDatabaseExists())
                {
                    return;
                }

                using var connection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=master;TrustServerCertificate=true;");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
                command.ExecuteNonQuery();
            }
            catch
            {
                // Ignoruj błędy podczas czyszczenia testowej bazy.
            }
        }
    }
}

