using System;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace WarsztatSamochodowy
{
    internal static class Program
    {
        private static void Main()
        {
            var databaseName = "WarsztatSamochodowyDB";
            var scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database", "init_database.sql"));
            var workshopDb = new WorkshopDatabase(databaseName);

            try
            {
                workshopDb.EnsureDatabaseInitialized(scriptPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd inicjalizacji bazy danych: " + ex.Message);
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("============================================");
                Console.WriteLine("    Obsługa warsztatu samochodowego v1.0    ");
                Console.WriteLine("============================================\n");
                Console.WriteLine("1. Pokaż pojazdy");
                Console.WriteLine("2. Pokaż zlecenia");
                Console.WriteLine("3. Dodaj nowe zlecenie");
                Console.WriteLine("4. Zamknij zlecenie");
                Console.WriteLine("5. Pokaż podsumowanie finansowe");
                Console.WriteLine("6. Pokaż klientów");
                Console.WriteLine("7. Dodaj klienta");
                Console.WriteLine("8. Pokaż usługi");
                Console.WriteLine("9. Pokaż części");
                Console.WriteLine("0. Wyjście\n");
                Console.Write("Wybierz opcję: ");

                var choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        ShowVehicles(workshopDb);
                        break;
                    case "2":
                        ShowOrders(workshopDb);
                        break;
                    case "3":
                        CreateOrder(workshopDb);
                        break;
                    case "4":
                        CloseOrder(workshopDb);
                        break;
                    case "5":
                        ShowFinance(workshopDb);
                        break;
                    case "6":
                        ShowClients(workshopDb);
                        break;
                    case "7":
                        AddClient(workshopDb);
                        break;
                    case "8":
                        ShowServices(workshopDb);
                        break;
                    case "9":
                        ShowParts(workshopDb);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Nieprawidłowa opcja. Naciśnij Enter, aby spróbować ponownie.");
                        break;
                }

                Console.WriteLine("\nNaciśnij Enter, aby kontynuować...");
                Console.ReadLine();
            }
        }

        private static void ShowVehicles(WorkshopDatabase db)
        {
            var sql = @"SELECT v.VIN, v.Nr_Rejestracyjny, v.Typ_Pojazdu, p.Nazwa AS Wlasciciel
FROM T_Pojazdy v
LEFT JOIN SL_Podmioty p ON v.ID_Podmiotu = p.ID_Podmiotu
ORDER BY v.Nr_Rejestracyjny";
            PrintTable(db.Query(sql));
        }

        private static void ShowOrders(WorkshopDatabase db)
        {
            var sql = @"SELECT z.ID_Zlecenia, z.VIN, z.Status, z.Forma_Platnosci, z.Data_Przyjecia, z.Planowana_Data_Zakonczenia, z.Data_Zakonczenia
FROM T_Zlecenia z
ORDER BY z.ID_Zlecenia";
            PrintTable(db.Query(sql));
        }

        private static void CreateOrder(WorkshopDatabase db)
        {
            var vehicles = db.Query(@"SELECT VIN, Nr_Rejestracyjny, Typ_Pojazdu FROM T_Pojazdy ORDER BY Nr_Rejestracyjny");
            if (vehicles.Rows.Count == 0)
            {
                Console.WriteLine("Brak zarejestrowanych pojazdów. Dodaj najpierw pojazd w bazie SQL.");
                return;
            }

            Console.WriteLine("Dostępne pojazdy:");
            PrintTable(vehicles);
            Console.Write("Podaj VIN pojazdu: ");
            var vin = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(vin))
            {
                Console.WriteLine("VIN nie może być pusty.");
                return;
            }

            if (!db.ValueExists("SELECT COUNT(*) FROM T_Pojazdy WHERE VIN = @vin", new SqlParameter("@vin", vin)))
            {
                Console.WriteLine("Pojazd o podanym VIN nie istnieje.");
                return;
            }

            Console.Write("Podaj formę płatności (np. Gotówka, Karta): ");
            var payment = Console.ReadLine()?.Trim() ?? string.Empty;
            Console.Write("Ile dni planujesz na wykonanie (domyślnie 3): ");
            var daysInput = Console.ReadLine()?.Trim();
            var days = 3;
            if (!string.IsNullOrWhiteSpace(daysInput) && int.TryParse(daysInput, out var parsedDays) && parsedDays > 0)
            {
                days = parsedDays;
            }

            const string sql = @"INSERT INTO T_Zlecenia (VIN, Planowana_Data_Zakonczenia, Forma_Platnosci, Status)
VALUES (@vin, DATEADD(day, @days, GETDATE()), @payment, 'Oczekujące')";
            var parameters = new[]
            {
                new SqlParameter("@vin", vin),
                new SqlParameter("@days", days),
                new SqlParameter("@payment", payment)
            };

            db.ExecuteNonQuery(sql, parameters);
            Console.WriteLine("Nowe zlecenie zostało dodane.");
        }

        private static void CloseOrder(WorkshopDatabase db)
        {
            var table = db.Query(@"SELECT ID_Zlecenia, VIN, Status FROM T_Zlecenia ORDER BY ID_Zlecenia");
            PrintTable(table);
            Console.Write("Podaj numer ID zlecenia do zamknięcia: ");
            var idInput = Console.ReadLine()?.Trim();
            if (!int.TryParse(idInput, out var orderId))
            {
                Console.WriteLine("Nieprawidłowe ID zlecenia.");
                return;
            }

            if (!db.ValueExists("SELECT COUNT(*) FROM T_Zlecenia WHERE ID_Zlecenia = @id", new SqlParameter("@id", orderId)))
            {
                Console.WriteLine("Zlecenie o podanym ID nie istnieje.");
                return;
            }

            db.ExecuteNonQuery(@"UPDATE T_Zlecenia SET Status = 'Gotowe', Data_Zakonczenia = GETDATE() WHERE ID_Zlecenia = @id", new SqlParameter("@id", orderId));
            Console.WriteLine("Zlecenie zostało zamknięte.");
        }

        private static void ShowFinance(WorkshopDatabase db)
        {
            var sql = @"SELECT ID_Zlecenia, VIN, Status, Suma_Rbg, Suma_Czesci, Suma_Uslug FROM v_FinanseZlecenia ORDER BY ID_Zlecenia";
            PrintTable(db.Query(sql));
        }

        private static void ShowClients(WorkshopDatabase db)
        {
            var sql = @"SELECT ID_Podmiotu, Typ_Podmiotu, Nazwa, Telefon, NIP FROM SL_Podmioty ORDER BY Typ_Podmiotu, Nazwa";
            PrintTable(db.Query(sql));
        }

        private static void AddClient(WorkshopDatabase db)
        {
            Console.Write("Typ (Klient / Dostawca): ");
            var typ = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(typ) || (typ != "Klient" && typ != "Dostawca"))
            {
                Console.WriteLine("Podaj poprawny typ: Klient lub Dostawca.");
                return;
            }

            Console.Write("Nazwa klienta / dostawcy: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Nazwa nie może być pusta.");
                return;
            }

            Console.Write("Telefon: ");
            var phone = Console.ReadLine()?.Trim() ?? string.Empty;
            Console.Write("NIP: ");
            var nip = Console.ReadLine()?.Trim() ?? string.Empty;

            const string sql = @"INSERT INTO SL_Podmioty (Typ_Podmiotu, Nazwa, Telefon, NIP)
VALUES (@typ, @nazwa, @telefon, @nip)";
            var parameters = new[]
            {
                new SqlParameter("@typ", typ),
                new SqlParameter("@nazwa", name),
                new SqlParameter("@telefon", phone),
                new SqlParameter("@nip", nip)
            };

            try
            {
                db.ExecuteNonQuery(sql, parameters);
                Console.WriteLine("Klient/dostawca został dodany.");
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Błąd dodawania klienta/dostawcy: " + ex.Message);
            }
        }

        private static void ShowServices(WorkshopDatabase db)
        {
            var sql = @"SELECT ID_Uslugi, Nazwa_Uslugi, Norma_Czasowa_Min, Cena_Uslugi FROM SL_Uslugi ORDER BY Nazwa_Uslugi";
            PrintTable(db.Query(sql));
        }

        private static void ShowParts(WorkshopDatabase db)
        {
            var sql = @"SELECT ID_Czesci, Nazwa_Czesci, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny FROM SL_Czesci ORDER BY Nazwa_Czesci";
            PrintTable(db.Query(sql));
        }

        private static void PrintTable(DataTable table)
        {
            if (table.Rows.Count == 0)
            {
                Console.WriteLine("Brak danych do wyświetlenia.");
                return;
            }

            var columnWidths = new int[table.Columns.Count];
            for (var i = 0; i < table.Columns.Count; i++)
            {
                columnWidths[i] = Math.Max(table.Columns[i].ColumnName.Length, 10);
            }

            foreach (DataRow row in table.Rows)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    columnWidths[i] = Math.Max(columnWidths[i], row[i]?.ToString()?.Length ?? 0);
                }
            }

            for (var i = 0; i < table.Columns.Count; i++)
            {
                Console.Write(table.Columns[i].ColumnName.PadRight(columnWidths[i] + 2));
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', columnWidths.Sum() + table.Columns.Count * 2));

            foreach (DataRow row in table.Rows)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    Console.Write((row[i]?.ToString() ?? string.Empty).PadRight(columnWidths[i] + 2));
                }

                Console.WriteLine();
            }
        }
    }
}
