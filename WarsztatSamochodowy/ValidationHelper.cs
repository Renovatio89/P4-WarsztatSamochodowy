using System;
using System.Linq;

namespace WarsztatSamochodowy
{
    public static class ValidationHelper
    {
        public static bool TryValidateClient(string? typ, string? name, string? phone, string? nip, out string error)
        {
            if (string.IsNullOrWhiteSpace(typ) || (typ != "Klient" && typ != "Dostawca"))
            {
                error = "Wybierz typ Klient lub Dostawca.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            {
                error = "Nazwa jest wymagana i nie może przekraczać 200 znaków.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var digits = new string(phone.Where(char.IsDigit).ToArray());
                if (digits.Length < 9 || digits.Length > 15)
                {
                    error = "Telefon musi zawierać od 9 do 15 cyfr.";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nip))
            {
                if (!nip.All(char.IsDigit) || nip.Length != 10)
                {
                    error = "NIP musi zawierać dokładnie 10 cyfr.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public static bool TryValidateVehicle(string? vin, string? ownerIdText, string? registration, string? type, out string error)
        {
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17 || !vin.All(ch => char.IsLetterOrDigit(ch)))
            {
                error = "VIN musi zawierać dokładnie 17 znaków alfanumerycznych.";
                return false;
            }

            if (!int.TryParse(ownerIdText, out var ownerId) || ownerId <= 0)
            {
                error = "ID klienta musi być liczbą większą od zera.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(registration) || registration.Length > 15)
            {
                error = "Numer rejestracyjny jest wymagany i nie może przekraczać 15 znaków.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(type) || type.Length > 100)
            {
                error = "Typ pojazdu jest wymagany i nie może przekraczać 100 znaków.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool TryValidateOrder(string? vin, string? payment, string? daysText, out string error)
        {
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17 || !vin.All(ch => char.IsLetterOrDigit(ch)))
            {
                error = "VIN musi zawierać dokładnie 17 znaków alfanumerycznych.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payment) || payment.Length > 50)
            {
                error = "Forma płatności jest wymagana i nie może przekraczać 50 znaków.";
                return false;
            }

            if (!int.TryParse(daysText, out var days) || days <= 0 || days > 365)
            {
                error = "Liczba dni musi być liczbą z zakresu 1-365.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
