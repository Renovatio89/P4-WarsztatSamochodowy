using System;
using System.IO;

namespace WarsztatSamochodowy
{
    internal static class Program
    {
        public static string FindSqlScriptPath()
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

        public static void InitializeDatabase(WorkshopDatabase workshopDb)
        {
            var scriptPath = FindSqlScriptPath();
            workshopDb.EnsureDatabaseInitialized(scriptPath);
        }
    }
}
