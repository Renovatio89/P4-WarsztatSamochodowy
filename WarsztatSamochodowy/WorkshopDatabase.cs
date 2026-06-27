using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace WarsztatSamochodowy
{
    public class WorkshopDatabase
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly string _masterConnectionString;

        public WorkshopDatabase(string databaseName)
        {
            _databaseName = databaseName;
            _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog={_databaseName};TrustServerCertificate=true;";
            _masterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=master;TrustServerCertificate=true;";
        }

        public bool DatabaseExists()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
            command.Parameters.AddWithValue("@name", _databaseName);
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar() ?? 0) > 0;
        }

        public void CreateDatabase()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{_databaseName}]";
            connection.Open();
            command.ExecuteNonQuery();
        }

        public void EnsureDatabaseInitialized(string sqlScriptPath)
        {
            if (!File.Exists(sqlScriptPath))
            {
                throw new FileNotFoundException("Plik skryptu SQL nie został znaleziony.", sqlScriptPath);
            }

            if (!DatabaseExists())
            {
                Console.WriteLine("Tworzę bazę danych...\n");
                CreateDatabase();
            }

            if (!DatabaseHasTables())
            {
                Console.WriteLine("Wczytuję strukturę bazy z pliku SQL...");
                ExecuteScriptFromFile(sqlScriptPath);
            }
            else
            {
                Console.WriteLine("Baza danych już zawiera tabele. Pomijam ponowną inicjalizację struktury.");
            }

            if (!HasSampleData())
            {
                Console.WriteLine("Wstawiam dane przykładowe...");
                InsertSampleData();
            }
        }

        private bool DatabaseHasTables()
        {
            const string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo'";
            return ExecuteScalar<int>(sql) > 0;
        }

        public bool HasSampleData()
        {
            const string sql = "SELECT COUNT(*) FROM sys.tables WHERE name = 'SL_Podmioty'";
            var tableCount = ExecuteScalar<int>(sql);
            if (tableCount == 0)
            {
                return false;
            }

            const string dataSql = "SELECT COUNT(*) FROM SL_Podmioty";
            return ExecuteScalar<int>(dataSql) > 0;
        }

        public void ExecuteScriptFromFile(string filePath)
        {
            var script = File.ReadAllText(filePath);
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.CommandText = batch;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        public DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.Parameters.AddRange(parameters);
            using var adapter = new SqlDataAdapter(command);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.Parameters.AddRange(parameters);
            connection.Open();
            return command.ExecuteNonQuery();
        }

        public T ExecuteScalar<T>(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.Parameters.AddRange(parameters);
            connection.Open();
            var result = command.ExecuteScalar();
            if (result == null || result is DBNull)
            {
                return default!;
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public bool ValueExists(string sql, params SqlParameter[] parameters)
        {
            return ExecuteScalar<int>(sql, parameters) > 0;
        }

        private void InsertSampleData()
        {
            var sql = @"
IF NOT EXISTS (SELECT 1 FROM SL_Podmioty WHERE NIP = '1112223334')
BEGIN
    INSERT INTO SL_Podmioty (Typ_Podmiotu, Nazwa, Telefon, NIP)
    VALUES ('Klient', 'AutoMoto Sp. z o.o.', '600000000', '1112223334');
END;

IF NOT EXISTS (SELECT 1 FROM SL_Podmioty WHERE NIP = '2223334445')
BEGIN
    INSERT INTO SL_Podmioty (Typ_Podmiotu, Nazwa, Telefon, NIP)
    VALUES ('Dostawca', 'CzesciAuto', '610000001', '2223334445');
END;

IF NOT EXISTS (SELECT 1 FROM SL_Pracownicy WHERE Imie = 'Jan' AND Nazwisko = 'Kowalski')
BEGIN
    INSERT INTO SL_Pracownicy (Imie, Nazwisko, Rola)
    VALUES ('Jan', 'Kowalski', 'Mechanik');
END;

IF NOT EXISTS (SELECT 1 FROM SL_Pracownicy WHERE Imie = 'Anna' AND Nazwisko = 'Nowak')
BEGIN
    INSERT INTO SL_Pracownicy (Imie, Nazwisko, Rola)
    VALUES ('Anna', 'Nowak', 'Księgowa');
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Olej silnikowy 5W30')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    VALUES (2, 'Olej silnikowy 5W30', 120.00, 200.00, 40, 5);
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Filtr powietrza')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    VALUES (2, 'Filtr powietrza', 25.00, 60.00, 20, 3);
END;

IF NOT EXISTS (SELECT 1 FROM SL_Uslugi WHERE Nazwa_Uslugi = 'Wymiana oleju')
BEGIN
    INSERT INTO SL_Uslugi (Nazwa_Uslugi, Norma_Czasowa_Min, Cena_Uslugi)
    VALUES ('Wymiana oleju', 30, 150.00);
END;

IF NOT EXISTS (SELECT 1 FROM SL_Uslugi WHERE Nazwa_Uslugi = 'Diagnostyka')
BEGIN
    INSERT INTO SL_Uslugi (Nazwa_Uslugi, Norma_Czasowa_Min, Cena_Uslugi)
    VALUES ('Diagnostyka', 90, 250.00);
END;

IF NOT EXISTS (SELECT 1 FROM T_Pojazdy WHERE VIN = '1HGBH41JXMN109186')
BEGIN
    INSERT INTO T_Pojazdy (VIN, ID_Podmiotu, Nr_Rejestracyjny, Typ_Pojazdu)
    VALUES ('1HGBH41JXMN109186', 1, 'WX12345', 'Sedan');
END;

IF NOT EXISTS (SELECT 1 FROM T_Pojazdy WHERE VIN = '2HNYD18885H000000')
BEGIN
    INSERT INTO T_Pojazdy (VIN, ID_Podmiotu, Nr_Rejestracyjny, Typ_Pojazdu)
    VALUES ('2HNYD18885H000000', 1, 'WN99999', 'SUV');
END;

IF NOT EXISTS (SELECT 1 FROM T_Zlecenia WHERE VIN = '1HGBH41JXMN109186' AND Status = 'Oczekujące')
BEGIN
    INSERT INTO T_Zlecenia (VIN, Planowana_Data_Zakonczenia, Forma_Platnosci, Status)
    VALUES ('1HGBH41JXMN109186', DATEADD(day, 5, GETDATE()), 'Gotówka', 'Oczekujące');
END;

IF NOT EXISTS (SELECT 1 FROM T_Zlecenia WHERE VIN = '2HNYD18885H000000' AND Status = 'Oczekujące')
BEGIN
    INSERT INTO T_Zlecenia (VIN, Planowana_Data_Zakonczenia, Forma_Platnosci, Status)
    VALUES ('2HNYD18885H000000', DATEADD(day, 2, GETDATE()), 'Karta', 'Oczekujące');
END;

IF NOT EXISTS (SELECT 1 FROM T_Zlecenia_Czesci WHERE ID_Zlecenia = 1 AND ID_Czesci = 1)
BEGIN
    INSERT INTO T_Zlecenia_Czesci (ID_Zlecenia, ID_Czesci, Ilosc, Cena_Transakcyjna)
    VALUES (1, 1, 2, 200.00);
END;

IF NOT EXISTS (SELECT 1 FROM T_Zlecenia_Czesci WHERE ID_Zlecenia = 2 AND ID_Czesci = 2)
BEGIN
    INSERT INTO T_Zlecenia_Czesci (ID_Zlecenia, ID_Czesci, Ilosc, Cena_Transakcyjna)
    VALUES (2, 2, 1, 60.00);
END;

IF NOT EXISTS (SELECT 1 FROM T_Zlecenia_Uslugi WHERE ID_Zlecenia = 1 AND ID_Uslugi = 1 AND ID_Pracownika = 1)
BEGIN
    INSERT INTO T_Zlecenia_Uslugi (ID_Zlecenia, ID_Uslugi, ID_Pracownika, Czas_Pracy_Min, Cena_Transakcyjna)
    VALUES (1, 1, 1, 30, 150.00);
END;

IF NOT EXISTS (SELECT 1 FROM T_Zlecenia_Uslugi WHERE ID_Zlecenia = 2 AND ID_Uslugi = 2 AND ID_Pracownika = 1)
BEGIN
    INSERT INTO T_Zlecenia_Uslugi (ID_Zlecenia, ID_Uslugi, ID_Pracownika, Czas_Pracy_Min, Cena_Transakcyjna)
    VALUES (2, 2, 1, 90, 250.00);
END;
";
            ExecuteNonQuery(sql);
        }
    }
}
