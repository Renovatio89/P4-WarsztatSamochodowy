# Obsługa warsztatu samochodowego

Prosty projekt konsolowy w C#, wykorzystujący bazę danych SQL z katalogu `Database/init_database.sql`.

## Co zawiera projekt

- `Database/init_database.sql` — struktura bazy danych, tabele, widok, procedury i triggery.
- `WarsztatSamochodowy/` — aplikacja konsolowa .NET 10, która łączy się z LocalDB.

## Jak uruchomić

1. Otwórz folder `P4-WarsztatSamochodowy` w Visual Studio Code.
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

6. W menu wybierz jedną z opcji, np. `1` aby wyświetlić pojazdy.

Aplikacja automatycznie:

- tworzy bazę danych `WarsztatSamochodowyDB` w LocalDB,
- wczytuje schemat z `Database/init_database.sql`,
- wstawia przykładowe dane,
- pozwala przeglądać pojazdy, klientów, usługi, części, zlecenia i podsumowanie finansowe.

## Jak wypchnąć na GitHub

1. Utwórz nowe repozytorium na GitHubie o nazwie `P4-WarsztatSamochodowy` w swoim koncie `Renovatio89`.
2. W folderze projektu `P4-WarsztatSamochodowy` wykonaj te polecenia:

```powershell
git init
git add .
git commit -m "Initial workshop application"
git branch -M main
git remote add origin https://github.com/Renovatio89/P4-WarsztatSamochodowy.git
git push -u origin main
```

3. Jeśli repozytorium już istnieje i masz ustawiony inny remote, użyj:

```powershell
git remote remove origin
git remote add origin https://github.com/Renovatio89/P4-WarsztatSamochodowy.git
git push -u origin main
```

## Wymagania

- .NET SDK 10
- SQL Server LocalDB (`(localdb)\\MSSQLLocalDB`)

## GitHub i wersjonowanie

Jeśli chcesz wypchnąć projekt do GitHuba:

```powershell
git init
git add .
git commit -m "Initial workshop application"
git branch -M main
git remote add origin https://github.com/<twoje-konto>/<repo>.git
git push -u origin main
```

## Co robi aplikacja

- inicjalizuje bazę danych i strukturę SQL,
- dodaje dane testowe,
- umożliwia tworzenie nowych zleceń,
- pozwala zamykać zlecenia,
- pokazuje klientów i pozwala dodawać nowych klientów,
- wyświetla listę usług i części,
- pokazuje podsumowanie z widoku finansowego.

> Używaj tego projektu jako prostego przykładu na zaliczenie. Nie dodawaj zaawansowanych funkcji, jeżeli chcesz mieć działającą wersję na czas.
