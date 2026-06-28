# Obsługa warsztatu samochodowego

Projekt desktopowy w C# z interfejsem okienkowym WPF, wykorzystujący bazę danych SQL z katalogu `Database/init_database.sql`.

Aplikacja wspiera podstawową obsługę warsztatu samochodowego: zarządzanie pojazdami, zleceniami, klientami, usługami, częściami oraz podsumowaniem finansowym. Została przygotowana jako prosta aplikacja na zaliczenie, działająca w Visual Studio Code i Visual Studio z LocalDB.

Aplikacja wykorzystuje ADO.NET do połączenia z bazą danych, automatycznie tworzy bazę danych w LocalDB, wczytuje strukturę z gotowego skryptu SQL oraz wstawia wprowadzone dane. Interfejs to klasyczne okno Windows z przyciskami i tabelą danych.

## Co zawiera projekt

- `Database/init_database.sql` — struktura bazy danych, tabele, widok, procedury i triggery.
- `WarsztatSamochodowy/` — aplikacja WPF .NET 10, która łączy się z LocalDB.

## Jak uruchomić

1. Otwórz folder `P4-WarsztatSamochodowy` w Visual Studio Code lub Visual Studio.
2. Otwórz terminal w VS Code.
3. Przejdź do katalogu projektu:

```powershell
cd .\WarsztatSamochodowy
```

4. Przywróć pakiety:

```powershell
dotnet restore
```

5. Uruchom aplikację:

```powershell
dotnet run
```

6. W głównym oknie kliknij odpowiedni przycisk, np. `Pokaż pojazdy`, aby wyświetlić dane.

Aplikacja automatycznie:

- tworzy bazę danych `WarsztatSamochodowyDB` w LocalDB,
- wczytuje schemat z `Database/init_database.sql`,
- wstawia przykładowe dane,
- pozwala przeglądać pojazdy, klientów, usługi, części, zlecenia i podsumowanie finansowe.

## Wymagania

- .NET SDK 10
- SQL Server LocalDB (`(localdb)\\MSSQLLocalDB`)
- Windows (ze względu na WPF)

## Co robi aplikacja

- uruchamia okno aplikacji WPF z przyciskami do nawigacji po danych,
- inicjalizuje bazę danych i strukturę SQL przy pierwszym uruchomieniu,
- wyświetla listę pojazdów, zleceń, klientów, usług i części,
- umożliwia dodawanie nowych zleceń, klientów i pojazdów,
- pozwala zamykać zlecenia i oznaczać je jako gotowe,
- pokazuje podsumowanie finansowe z widoku finansowego,
- waliduje dane wprowadzane przez użytkownika przed zapisaniem do bazy.
