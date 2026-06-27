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

            Console.WriteLine("Wstawiam lub odświeżam dane przykładowe...");
            InsertSampleData();
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
-- Przykładowi klienci i dostawcy
IF NOT EXISTS (SELECT 1 FROM SL_Podmioty WHERE NIP = '1112223334')
BEGIN
    INSERT INTO SL_Podmioty (Typ_Podmiotu, Nazwa, Telefon, NIP) VALUES
    ('Klient', 'Jan Kowalski', '500-111-222', '1112223334'),
    ('Klient', 'Anna Nowak', '600-222-333', '1112223335'),
    ('Klient', 'Piotr Zieliński', '600-333-444', '1112223336'),
    ('Klient', 'Marek Dąbrowski', '600-444-555', '1112223337'),
    ('Klient', 'Marta Wiśniewska', '600-555-666', '1112223338'),
    ('Klient', 'Firma AutoExpress', '32-222-333', '1112223339'),
    ('Klient', 'Moto Serwis', '32-333-444', '1112223340'),
    ('Dostawca', 'Auto-Części Hurtownia', '33-111-00-00', '9330987654'),
    ('Dostawca', 'Oleje Silesia', '33-222-11-11', '5479998877'),
    ('Dostawca', 'Pneumatik Sp. z o.o.', '33-333-22-22', '5478887766');
END;

IF NOT EXISTS (SELECT 1 FROM SL_Pracownicy WHERE Imie = 'Adam' AND Nazwisko = 'Wiśniewski')
BEGIN
    INSERT INTO SL_Pracownicy (Imie, Nazwisko, Rola) VALUES
    ('Adam', 'Wiśniewski', 'Mechanik - Silniki'),
    ('Tomasz', 'Kamiński', 'Mechanik - Zawieszenie'),
    ('Piotr', 'Lewandowski', 'Kierownik Warsztatu'),
    ('Marta', 'Zielińska', 'Recepcja');
END;

IF NOT EXISTS (SELECT 1 FROM SL_Uslugi WHERE Nazwa_Uslugi = 'Wymiana klocków i tarcz hamulcowych (oś)')
BEGIN
    INSERT INTO SL_Uslugi (Nazwa_Uslugi, Norma_Czasowa_Min, Cena_Uslugi) VALUES
    ('Wymiana klocków i tarcz hamulcowych (oś)', 90, 180.00),
    ('Wymiana oleju silnikowego z filtrem', 40, 80.00),
    ('Diagnostyka komputerowa silnika', 30, 150.00),
    ('Wymiana rozrządu', 240, 600.00),
    ('Przegląd przedsezonowy', 60, 120.00);
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Klocki hamulcowe przód')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Klocki hamulcowe przód', 80.00, 140.00, 15, 4 FROM SL_Podmioty WHERE NIP = '9330987654';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Tarcze hamulcowe przód (komplet)')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Tarcze hamulcowe przód (komplet)', 150.00, 250.00, 10, 2 FROM SL_Podmioty WHERE NIP = '9330987654';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Olej syntetyczny 5W30 5L')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Olej syntetyczny 5W30 5L', 95.00, 160.00, 20, 5 FROM SL_Podmioty WHERE NIP = '5479998877';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Filtr oleju standard')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Filtr oleju standard', 15.00, 35.00, 30, 10 FROM SL_Podmioty WHERE NIP = '5479998877';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Zestaw paska rozrządu')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Zestaw paska rozrządu', 300.00, 450.00, 5, 1 FROM SL_Podmioty WHERE NIP = '5478887766';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Świece zapłonowe')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Świece zapłonowe', 10.00, 30.00, 50, 10 FROM SL_Podmioty WHERE NIP = '9330987654';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Płyn hamulcowy DOT4')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Płyn hamulcowy DOT4', 25.00, 55.00, 25, 5 FROM SL_Podmioty WHERE NIP = '5478887766';
END;

IF NOT EXISTS (SELECT 1 FROM SL_Czesci WHERE Nazwa_Czesci = 'Filtr powietrza sport')
BEGIN
    INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny)
    SELECT TOP 1 ID_Podmiotu, 'Filtr powietrza sport', 40.00, 90.00, 15, 3 FROM SL_Podmioty WHERE NIP = '5479998877';
END;

DECLARE @i INT = 1;
WHILE @i <= 20
BEGIN
    DECLARE @vin VARCHAR(17) = CONCAT('VIN', RIGHT('000000000000000' + CAST(@i AS VARCHAR(10)), 14));
    DECLARE @ownerNip VARCHAR(15) = CASE WHEN @i % 5 = 1 THEN '1112223334' WHEN @i % 5 = 2 THEN '1112223335' WHEN @i % 5 = 3 THEN '1112223336' WHEN @i % 5 = 4 THEN '1112223337' ELSE '1112223338' END;
    DECLARE @ownerId INT = (SELECT TOP 1 ID_Podmiotu FROM SL_Podmioty WHERE NIP = @ownerNip);
    IF @ownerId IS NULL
        SET @ownerId = (SELECT TOP 1 ID_Podmiotu FROM SL_Podmioty WHERE Typ_Podmiotu = 'Klient');
    DECLARE @reg VARCHAR(15) = CONCAT('SBE', RIGHT('00000' + CAST(@i AS VARCHAR(10)), 5));
    DECLARE @type VARCHAR(100) = CASE @i
        WHEN 1 THEN 'Audi A4 B8 2.0 TDI'
        WHEN 2 THEN 'Toyota Corolla 1.6 VVT-i'
        WHEN 3 THEN 'Skoda Octavia III 1.4 TSI'
        WHEN 4 THEN 'BMW 320d E90'
        WHEN 5 THEN 'Ford Focus 1.6'
        WHEN 6 THEN 'Opel Astra 1.7 CDTI'
        WHEN 7 THEN 'Renault Megane 1.5 dCi'
        WHEN 8 THEN 'Volkswagen Golf VII 1.6 TDI'
        WHEN 9 THEN 'Peugeot 308 1.2 PureTech'
        WHEN 10 THEN 'Hyundai i30 1.4 MPI'
        WHEN 11 THEN 'Seat Leon 1.4 TSI'
        WHEN 12 THEN 'Mazda 3 2.0 SkyActiv'
        WHEN 13 THEN 'Nissan Qashqai 1.5 dCi'
        WHEN 14 THEN 'Kia Ceed 1.6 GDI'
        WHEN 15 THEN 'Honda Civic 1.6 i-DTEC'
        WHEN 16 THEN 'Mitsubishi Outlander 2.0'
        WHEN 17 THEN 'Suzuki Swift 1.2'
        WHEN 18 THEN 'Citroen C4 1.6 HDi'
        WHEN 19 THEN 'Mercedes-Benz C200 2.0'
        ELSE 'Volvo V40 1.6 D'
    END;

    IF NOT EXISTS (SELECT 1 FROM T_Pojazdy WHERE VIN = @vin)
    BEGIN
        INSERT INTO T_Pojazdy (VIN, ID_Podmiotu, Nr_Rejestracyjny, Typ_Pojazdu)
        VALUES (@vin, @ownerId, @reg, @type);
    END;

    SET @i += 1;
END;

DECLARE @j INT = 1;
WHILE @j <= 20
BEGIN
    DECLARE @vin2 VARCHAR(17) = CONCAT('VIN', RIGHT('000000000000000' + CAST(@j AS VARCHAR(10)), 14));
    DECLARE @payment VARCHAR(50) = CASE WHEN @j % 2 = 0 THEN 'Karta' ELSE 'Gotówka' END;
    DECLARE @status VARCHAR(50) = CASE WHEN @j % 3 = 0 THEN 'Gotowe' WHEN @j % 3 = 1 THEN 'W trakcie' ELSE 'Oczekujące' END;

    IF NOT EXISTS (SELECT 1 FROM T_Zlecenia WHERE VIN = @vin2 AND Forma_Platnosci = @payment AND Status = @status)
    BEGIN
        INSERT INTO T_Zlecenia (VIN, Data_Przyjecia, Status, Forma_Platnosci)
        VALUES (@vin2, DATEADD(DAY, -(@j % 10), GETDATE()), @status, @payment);
    END;

    SET @j += 1;
END;
";
            ExecuteNonQuery(sql);
        }
    }
}
