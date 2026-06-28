using System;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace WarsztatSamochodowy
{
    public partial class MainWindow : Window
    {
        private readonly WorkshopDatabase _db;

        public MainWindow()
        {
            InitializeComponent();
            var databaseName = "WarsztatSamochodowyDB";
            _db = new WorkshopDatabase(databaseName);

            try
            {
                Program.InitializeDatabase(_db);
                StatusText.Text = "Połączenie z bazą danych zostało zainicjowane.";
                ShowVehicles();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Błąd inicjalizacji bazy danych: " + ex.Message;
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowVehicles_Click(object sender, RoutedEventArgs e) => ShowVehicles();
        private void ShowOrders_Click(object sender, RoutedEventArgs e) => ShowOrders();
        private void CreateOrder_Click(object sender, RoutedEventArgs e) => CreateOrder();
        private void CloseOrder_Click(object sender, RoutedEventArgs e) => CloseOrder();
        private void ShowFinance_Click(object sender, RoutedEventArgs e) => ShowFinance();
        private void ShowClients_Click(object sender, RoutedEventArgs e) => ShowClients();
        private void AddClient_Click(object sender, RoutedEventArgs e) => AddClient();
        private void ShowServices_Click(object sender, RoutedEventArgs e) => ShowServices();
        private void ShowParts_Click(object sender, RoutedEventArgs e) => ShowParts();
        private void AddVehicle_Click(object sender, RoutedEventArgs e) => AddVehicle();

        private void ShowVehicles()
        {
            var sql = @"SELECT v.VIN, v.Nr_Rejestracyjny, v.Typ_Pojazdu, p.Nazwa AS Wlasciciel
FROM T_Pojazdy v
LEFT JOIN SL_Podmioty p ON v.ID_Podmiotu = p.ID_Podmiotu
ORDER BY v.Nr_Rejestracyjny";
            LoadData(sql, "Pojazdy");
        }

        private void ShowOrders()
        {
            var sql = @"SELECT z.ID_Zlecenia, z.VIN, z.Status, z.Forma_Platnosci, z.Data_Przyjecia, z.Planowana_Data_Zakonczenia, z.Data_Zakonczenia
FROM T_Zlecenia z
ORDER BY z.ID_Zlecenia";
            LoadData(sql, "Zlecenia");
        }

        private void CreateOrder()
        {
            var vehicles = _db.Query(@"SELECT VIN, Nr_Rejestracyjny, Typ_Pojazdu FROM T_Pojazdy ORDER BY Nr_Rejestracyjny");
            if (vehicles.Rows.Count == 0)
            {
                MessageBox.Show("Brak zarejestrowanych pojazdów. Dodaj najpierw pojazd.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Dodaj zlecenie",
                Width = 420,
                Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(12) };
            var vinBox = new System.Windows.Controls.TextBox();
            var paymentBox = new System.Windows.Controls.TextBox();
            var daysBox = new System.Windows.Controls.TextBox { Text = "3" };
            var submit = new System.Windows.Controls.Button { Content = "Dodaj", Margin = new Thickness(0, 12, 0, 0) };

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "VIN pojazdu:" });
            panel.Children.Add(vinBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Forma płatności:" });
            panel.Children.Add(paymentBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Liczba dni:" });
            panel.Children.Add(daysBox);
            panel.Children.Add(submit);
            dialog.Content = panel;

            submit.Click += (_, _) =>
            {
                var vin = vinBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(vin))
                {
                    MessageBox.Show("VIN nie może być pusty.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_db.ValueExists("SELECT COUNT(*) FROM T_Pojazdy WHERE VIN = @vin", new SqlParameter("@vin", vin)))
                {
                    MessageBox.Show("Pojazd o podanym VIN nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int days = 3;
                if (!int.TryParse(daysBox.Text.Trim(), out days) || days <= 0)
                {
                    days = 3;
                }

                const string sql = @"INSERT INTO T_Zlecenia (VIN, Planowana_Data_Zakonczenia, Forma_Platnosci, Status)
VALUES (@vin, DATEADD(day, @days, GETDATE()), @payment, 'Oczekujące')";
                var parameters = new[]
                {
                    new SqlParameter("@vin", vin),
                    new SqlParameter("@days", days),
                    new SqlParameter("@payment", paymentBox.Text.Trim())
                };

                _db.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Zlecenie zostało dodane.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                dialog.Close();
                ShowOrders();
            };

            dialog.ShowDialog();
        }

        private void CloseOrder()
        {
            var table = _db.Query(@"SELECT ID_Zlecenia, VIN, Status FROM T_Zlecenia ORDER BY ID_Zlecenia");
            if (table.Rows.Count == 0)
            {
                MessageBox.Show("Brak zleceń do zamknięcia.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Zamknij zlecenie",
                Width = 320,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(12) };
            var idBox = new System.Windows.Controls.TextBox();
            var submit = new System.Windows.Controls.Button { Content = "Zamknij", Margin = new Thickness(0, 12, 0, 0) };
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Podaj ID zlecenia:" });
            panel.Children.Add(idBox);
            panel.Children.Add(submit);
            dialog.Content = panel;

            submit.Click += (_, _) =>
            {
                if (!int.TryParse(idBox.Text.Trim(), out var orderId))
                {
                    MessageBox.Show("Nieprawidłowe ID zlecenia.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_db.ValueExists("SELECT COUNT(*) FROM T_Zlecenia WHERE ID_Zlecenia = @id", new SqlParameter("@id", orderId)))
                {
                    MessageBox.Show("Zlecenie o podanym ID nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _db.ExecuteNonQuery(@"UPDATE T_Zlecenia SET Status = 'Gotowe', Data_Zakonczenia = GETDATE() WHERE ID_Zlecenia = @id", new SqlParameter("@id", orderId));
                MessageBox.Show("Zlecenie zostało zamknięte.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                dialog.Close();
                ShowOrders();
            };

            dialog.ShowDialog();
        }

        private void ShowFinance()
        {
            var sql = @"SELECT ID_Zlecenia, VIN, Status, Suma_Rbg, Suma_Czesci, Suma_Uslug FROM v_FinanseZlecenia ORDER BY ID_Zlecenia";
            LoadData(sql, "Finanse");
        }

        private void ShowClients()
        {
            var sql = @"SELECT ID_Podmiotu, Typ_Podmiotu, Nazwa, Telefon, NIP FROM SL_Podmioty ORDER BY Typ_Podmiotu, Nazwa";
            LoadData(sql, "Klienci");
        }

        private void AddClient()
        {
            var dialog = new Window
            {
                Title = "Dodaj klienta / dostawcę",
                Width = 420,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(12) };
            var typeBox = new System.Windows.Controls.ComboBox { ItemsSource = new[] { "Klient", "Dostawca" } };
            var nameBox = new System.Windows.Controls.TextBox();
            var phoneBox = new System.Windows.Controls.TextBox();
            var nipBox = new System.Windows.Controls.TextBox();
            var submit = new System.Windows.Controls.Button { Content = "Dodaj", Margin = new Thickness(0, 12, 0, 0) };

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Typ:" });
            panel.Children.Add(typeBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Nazwa:" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Telefon:" });
            panel.Children.Add(phoneBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "NIP:" });
            panel.Children.Add(nipBox);
            panel.Children.Add(submit);
            dialog.Content = panel;

            submit.Click += (_, _) =>
            {
                var typ = typeBox.SelectedItem?.ToString();
                var name = nameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(typ) || string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Uzupełnij typ i nazwę.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                const string sql = @"INSERT INTO SL_Podmioty (Typ_Podmiotu, Nazwa, Telefon, NIP)
VALUES (@typ, @nazwa, @telefon, @nip)";
                var parameters = new[]
                {
                    new SqlParameter("@typ", typ),
                    new SqlParameter("@nazwa", name),
                    new SqlParameter("@telefon", phoneBox.Text.Trim()),
                    new SqlParameter("@nip", nipBox.Text.Trim())
                };

                try
                {
                    _db.ExecuteNonQuery(sql, parameters);
                    MessageBox.Show("Klient/dostawca został dodany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    dialog.Close();
                    ShowClients();
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Błąd dodawania: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            dialog.ShowDialog();
        }

        private void ShowServices()
        {
            var sql = @"SELECT ID_Uslugi, Nazwa_Uslugi, Norma_Czasowa_Min, Cena_Uslugi FROM SL_Uslugi ORDER BY Nazwa_Uslugi";
            LoadData(sql, "Usługi");
        }

        private void ShowParts()
        {
            var sql = @"SELECT ID_Czesci, Nazwa_Czesci, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny FROM SL_Czesci ORDER BY Nazwa_Czesci";
            LoadData(sql, "Części");
        }

        private void AddVehicle()
        {
            var dialog = new Window
            {
                Title = "Dodaj pojazd",
                Width = 440,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(12) };
            var vinBox = new System.Windows.Controls.TextBox();
            var ownerBox = new System.Windows.Controls.TextBox();
            var registrationBox = new System.Windows.Controls.TextBox();
            var typeBox = new System.Windows.Controls.TextBox();
            var submit = new System.Windows.Controls.Button { Content = "Dodaj", Margin = new Thickness(0, 12, 0, 0) };

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "VIN:" });
            panel.Children.Add(vinBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "ID klienta:" });
            panel.Children.Add(ownerBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Numer rejestracyjny:" });
            panel.Children.Add(registrationBox);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Typ pojazdu:" });
            panel.Children.Add(typeBox);
            panel.Children.Add(submit);
            dialog.Content = panel;

            submit.Click += (_, _) =>
            {
                var vin = vinBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(vin) || vin.Length > 17)
                {
                    MessageBox.Show("Podaj poprawny VIN (maksymalnie 17 znaków).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_db.ValueExists("SELECT COUNT(*) FROM SL_Podmioty WHERE ID_Podmiotu = @id AND Typ_Podmiotu = 'Klient'", new SqlParameter("@id", ownerBox.Text.Trim())))
                {
                    MessageBox.Show("Nie znaleziono klienta o podanym ID.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                const string sql = @"INSERT INTO T_Pojazdy (VIN, ID_Podmiotu, Nr_Rejestracyjny, Typ_Pojazdu)
VALUES (@vin, @ownerId, @reg, @type)";
                var parameters = new[]
                {
                    new SqlParameter("@vin", vin),
                    new SqlParameter("@ownerId", int.Parse(ownerBox.Text.Trim())),
                    new SqlParameter("@reg", registrationBox.Text.Trim()),
                    new SqlParameter("@type", typeBox.Text.Trim())
                };

                try
                {
                    _db.ExecuteNonQuery(sql, parameters);
                    MessageBox.Show("Pojazd został dodany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    dialog.Close();
                    ShowVehicles();
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Błąd dodawania: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            dialog.ShowDialog();
        }

        private void LoadData(string sql, string title)
        {
            try
            {
                var table = _db.Query(sql);
                DataGridResults.ItemsSource = table.DefaultView;
                StatusText.Text = $"Wyświetlam: {title}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Błąd pobierania danych: " + ex.Message;
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
