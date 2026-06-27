/*******************************************************************************
 PROJEKT: System wspomagania zarządzania warsztatem samochodowym
 ZAWARTOŚĆ: Pełny skrypt DDL, DML oraz Programowalność (Widoki, Procedury, Triggery)
 *******************************************************************************/

-- =============================================================================
-- 1. SEKCJA CZYSZCZĄCA (DROP)
-- =============================================================================
IF OBJECT_ID('tr_Blokada_Edycji_Uslug', 'TR') IS NOT NULL DROP TRIGGER tr_Blokada_Edycji_Uslug;
IF OBJECT_ID('tr_Zlecenia_Czesci_Cena', 'TR') IS NOT NULL DROP TRIGGER tr_Zlecenia_Czesci_Cena;
IF OBJECT_ID('tr_Magazyn_Odejmij', 'TR') IS NOT NULL DROP TRIGGER tr_Magazyn_Odejmij;
IF OBJECT_ID('v_FinanseZlecenia', 'V') IS NOT NULL DROP VIEW v_FinanseZlecenia;
IF OBJECT_ID('sp_ZamknijZlecenie', 'P') IS NOT NULL DROP PROCEDURE sp_ZamknijZlecenie;
IF OBJECT_ID('dbo.fn_MinutyNaGodziny', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_MinutyNaGodziny;

IF OBJECT_ID('T_Zlecenia_Uslugi', 'U') IS NOT NULL DROP TABLE T_Zlecenia_Uslugi;
IF OBJECT_ID('T_Zlecenia_Czesci', 'U') IS NOT NULL DROP TABLE T_Zlecenia_Czesci;
IF OBJECT_ID('T_Zlecenia', 'U')        IS NOT NULL DROP TABLE T_Zlecenia;
IF OBJECT_ID('T_Pojazdy', 'U')         IS NOT NULL DROP TABLE T_Pojazdy;
IF OBJECT_ID('SL_Czesci', 'U')         IS NOT NULL DROP TABLE SL_Czesci;
IF OBJECT_ID('SL_Uslugi', 'U')         IS NOT NULL DROP TABLE SL_Uslugi;
IF OBJECT_ID('SL_Pracownicy', 'U')     IS NOT NULL DROP TABLE SL_Pracownicy;
IF OBJECT_ID('SL_Podmioty', 'U')       IS NOT NULL DROP TABLE SL_Podmioty;
GO

-- =============================================================================
-- 2. TWORZENIE TABEL (DDL)
-- =============================================================================
CREATE TABLE SL_Podmioty (
    ID_Podmiotu INT IDENTITY(1,1) PRIMARY KEY,
    Typ_Podmiotu VARCHAR(20) NOT NULL,
    Nazwa VARCHAR(200) NOT NULL,
    Telefon VARCHAR(20),
    NIP VARCHAR(15) UNIQUE
);

CREATE TABLE SL_Pracownicy (
    ID_Pracownika INT IDENTITY(1,1) PRIMARY KEY,
    Imie VARCHAR(50) NOT NULL,
    Nazwisko VARCHAR(50) NOT NULL,
    Rola VARCHAR(50)
);

CREATE TABLE SL_Czesci (
    ID_Czesci INT IDENTITY(1,1) PRIMARY KEY,
    ID_Podmiotu INT,
    Nazwa_Czesci VARCHAR(200) NOT NULL,
    Cena_Zakupu DECIMAL(18,2) DEFAULT 0,
    Cena_Sprzedazy DECIMAL(18,2) DEFAULT 0,
    Stan_Aktualny INT DEFAULT 0,
    Stan_Minimalny INT DEFAULT 0,
    CONSTRAINT FK_Czesci_Podmioty FOREIGN KEY (ID_Podmiotu) REFERENCES SL_Podmioty(ID_Podmiotu),
    CONSTRAINT CHK_Czesci_Ceny CHECK (Cena_Zakupu >= 0 AND Cena_Sprzedazy >= 0),
    CONSTRAINT CHK_Czesci_Stany CHECK (Stan_Aktualny >= 0 AND Stan_Minimalny >= 0)
);

CREATE TABLE SL_Uslugi (
    ID_Uslugi INT IDENTITY(1,1) PRIMARY KEY,
    Nazwa_Uslugi VARCHAR(200) NOT NULL,
    Norma_Czasowa_Min INT DEFAULT 0,
    Cena_Uslugi DECIMAL(18,2) DEFAULT 0,
    CONSTRAINT CHK_Uslugi_Cena CHECK (Cena_Uslugi >= 0),
    CONSTRAINT CHK_Uslugi_Czas CHECK (Norma_Czasowa_Min >= 0)
);

CREATE TABLE T_Pojazdy (
    VIN VARCHAR(17) PRIMARY KEY,
    ID_Podmiotu INT NOT NULL,
    Nr_Rejestracyjny VARCHAR(15) UNIQUE,
    Typ_Pojazdu VARCHAR(100),
    CONSTRAINT FK_Pojazdy_Podmioty FOREIGN KEY (ID_Podmiotu) REFERENCES SL_Podmioty(ID_Podmiotu)
);

CREATE TABLE T_Zlecenia (
    ID_Zlecenia INT IDENTITY(1,1) PRIMARY KEY,
    VIN VARCHAR(17) NOT NULL,
    Data_Przyjecia DATETIME NOT NULL DEFAULT GETDATE(),
    Planowana_Data_Zakonczenia DATETIME,
    Data_Zakonczenia DATETIME,
    Status VARCHAR(50) DEFAULT 'Oczekujące',
    Forma_Platnosci VARCHAR(50),
    CONSTRAINT FK_Zlecenia_Pojazdy FOREIGN KEY (VIN) REFERENCES T_Pojazdy(VIN),
    CONSTRAINT CHK_Zlecenia_Daty CHECK (Data_Zakonczenia >= Data_Przyjecia OR Data_Zakonczenia IS NULL)
);

CREATE TABLE T_Zlecenia_Czesci (
    ID_Zlec_Czesc INT IDENTITY(1,1) PRIMARY KEY,
    ID_Zlecenia INT NOT NULL,
    ID_Czesci INT NOT NULL,
    Ilosc INT NOT NULL,
    Cena_Transakcyjna DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_ZC_Zlecenia FOREIGN KEY (ID_Zlecenia) REFERENCES T_Zlecenia(ID_Zlecenia),
    CONSTRAINT FK_ZC_Czesci FOREIGN KEY (ID_Czesci) REFERENCES SL_Czesci(ID_Czesci),
    CONSTRAINT CHK_ZC_Ilosc CHECK (Ilosc > 0),
    CONSTRAINT CHK_ZC_Cena CHECK (Cena_Transakcyjna >= 0)
);

CREATE TABLE T_Zlecenia_Uslugi (
    ID_Zlec_Usluga INT IDENTITY(1,1) PRIMARY KEY,
    ID_Zlecenia INT NOT NULL,
    ID_Uslugi INT NOT NULL,
    ID_Pracownika INT NOT NULL,
    Czas_Pracy_Min INT DEFAULT 0,
    Cena_Transakcyjna DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_ZU_Zlecenia FOREIGN KEY (ID_Zlecenia) REFERENCES T_Zlecenia(ID_Zlecenia),
    CONSTRAINT FK_ZU_Uslugi FOREIGN KEY (ID_Uslugi) REFERENCES SL_Uslugi(ID_Uslugi),
    CONSTRAINT FK_ZU_Pracownicy FOREIGN KEY (ID_Pracownika) REFERENCES SL_Pracownicy(ID_Pracownika),
    CONSTRAINT CHK_ZU_Czas CHECK (Czas_Pracy_Min >= 0),
    CONSTRAINT CHK_ZU_Cena CHECK (Cena_Transakcyjna >= 0)
);
GO

-- =============================================================================
-- 3. OBIEKTY PROGRAMISTYCZNE (FUNKCJE, WIDOKI, PROCEDURY, TRIGGERY)
-- =============================================================================

-- Funkcja: Przeliczanie minut na godziny
CREATE FUNCTION dbo.fn_MinutyNaGodziny (@minuty INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    RETURN CAST(@minuty AS DECIMAL(10,2)) / 60.0;
END;
GO

-- Widok: Podsumowanie finansowe zleceń
CREATE VIEW v_FinanseZlecenia AS
SELECT 
    z.ID_Zlecenia,
    z.VIN,
    z.Status,
    dbo.fn_MinutyNaGodziny(ISNULL(SUM(zu.Czas_Pracy_Min), 0)) AS Suma_Rbg,
    ISNULL(SUM(zc.Ilosc * zc.Cena_Transakcyjna), 0) AS Suma_Czesci,
    ISNULL(SUM(zu.Czas_Pracy_Min * (zu.Cena_Transakcyjna/60.0)), 0) AS Suma_Uslug
FROM T_Zlecenia z
LEFT JOIN T_Zlecenia_Czesci zc ON z.ID_Zlecenia = zc.ID_Zlecenia
LEFT JOIN T_Zlecenia_Uslugi zu ON z.ID_Zlecenia = zu.ID_Zlecenia
GROUP BY z.ID_Zlecenia, z.VIN, z.Status;
GO

-- Procedura: Zamknięcie zlecenia
CREATE PROCEDURE sp_ZamknijZlecenie
    @ID_Zlecenia INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE T_Zlecenia 
    SET Status = 'Gotowe', Data_Zakonczenia = GETDATE()
    WHERE ID_Zlecenia = @ID_Zlecenia;
END;
GO

-- Trigger 1: Odejmowanie części ze stanu
CREATE TRIGGER tr_Magazyn_Odejmij ON T_Zlecenia_Czesci
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE SL_Czesci
    SET Stan_Aktualny = Stan_Aktualny - i.Ilosc
    FROM SL_Czesci c
    JOIN inserted i ON c.ID_Czesci = i.ID_Czesci;
END;
GO

-- Trigger 2: Zamrażanie ceny części
CREATE TRIGGER tr_Zlecenia_Czesci_Cena ON T_Zlecenia_Czesci
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE T_Zlecenia_Czesci
    SET Cena_Transakcyjna = c.Cena_Sprzedazy
    FROM T_Zlecenia_Czesci zc
    JOIN inserted i ON zc.ID_Zlec_Czesc = i.ID_Zlec_Czesc
    JOIN SL_Czesci c ON i.ID_Czesci = c.ID_Czesci;
END;
GO

-- Trigger 3: Blokada modyfikacji zamkniętych zleceń
CREATE TRIGGER tr_Blokada_Edycji_Uslug ON T_Zlecenia_Uslugi
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM T_Zlecenia z
        JOIN deleted d ON z.ID_Zlecenia = d.ID_Zlecenia
        WHERE z.Status IN ('Gotowe', 'Opłacone')
    )
    BEGIN
        RAISERROR ('Błąd: Nie można modyfikować zakresu prac w zleceniu archiwalnym!', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- =============================================================================
-- 4. WPROWADZANIE DANYCH TESTOWYCH (DML - INSERT)
-- =============================================================================

-- Słownik Podmiotów (Klienci i Dostawcy)
INSERT INTO SL_Podmioty (Typ_Podmiotu, Nazwa, Telefon, NIP) VALUES 
('Klient', 'Jan Kowalski', '500-111-222', NULL),
('Klient', 'Anna Nowak', '600-222-333', NULL),
('Klient', 'Trans-Bud Sp. z o.o.', '33-444-55-66', '5471234567'),
('Dostawca', 'Auto-Części Hurtownia', '33-111-00-00', '9330987654'),
('Dostawca', 'Oleje Silesia', '33-222-11-11', '5479998877');

-- Słownik Pracowników
INSERT INTO SL_Pracownicy (Imie, Nazwisko, Rola) VALUES 
('Adam', 'Wiśniewski', 'Mechanik - Silniki'),
('Tomasz', 'Kamiński', 'Mechanik - Zawieszenie'),
('Piotr', 'Lewandowski', 'Kierownik Warsztatu'),
('Marta', 'Zielińska', 'Recepcja');

-- Słownik Usług
INSERT INTO SL_Uslugi (Nazwa_Uslugi, Norma_Czasowa_Min, Cena_Uslugi) VALUES 
('Wymiana klocków i tarcz hamulcowych (oś)', 90, 180.00),
('Wymiana oleju silnikowego z filtrem', 40, 80.00),
('Diagnostyka komputerowa silnika', 30, 150.00),
('Wymiana rozrządu', 240, 600.00),
('Przegląd przedsezonowy', 60, 120.00);

-- Słownik Części
INSERT INTO SL_Czesci (ID_Podmiotu, Nazwa_Czesci, Cena_Zakupu, Cena_Sprzedazy, Stan_Aktualny, Stan_Minimalny) VALUES 
(4, 'Klocki hamulcowe przód', 80.00, 140.00, 15, 4),
(4, 'Tarcze hamulcowe przód (komplet)', 150.00, 250.00, 10, 2),
(5, 'Olej syntetyczny 5W30 5L', 95.00, 160.00, 20, 5),
(5, 'Filtr oleju standard', 15.00, 35.00, 30, 10),
(4, 'Zestaw paska rozrządu', 300.00, 450.00, 5, 1);

-- Ewidencja Pojazdów
INSERT INTO T_Pojazdy (VIN, ID_Podmiotu, Nr_Rejestracyjny, Typ_Pojazdu) VALUES 
('VIN1234567890AUDI', 1, 'SBE 12345', 'Audi A4 B8 2.0 TDI'),
('VIN9876543210TOYO', 2, 'SZY 98765', 'Toyota Corolla 1.6 VVT-i'),
('VIN1112223330SKOD', 3, 'SB 44444', 'Skoda Octavia III 1.4 TSI'),
('VIN9998887770BMWX', 1, 'SBE 99999', 'BMW 320d E90');

-- Rejestr Zleceń (Główne nagłówki napraw)
INSERT INTO T_Zlecenia (VIN, Data_Przyjecia, Status, Forma_Platnosci) VALUES 
('VIN1234567890AUDI', DATEADD(DAY, -2, GETDATE()), 'W trakcie', 'Karta'),
('VIN9876543210TOYO', DATEADD(DAY, -1, GETDATE()), 'Oczekujące', 'Gotówka'),
('VIN1112223330SKOD', DATEADD(DAY, -5, GETDATE()), 'Gotowe', 'Przelew'),
('VIN9998887770BMWX', GETDATE(), 'Oczekujące', 'Karta');

-- Pozycje Zleceń - Wykorzystane Części 
-- Uwaga: Wpisujemy Cena_Transakcyjna = 0, ponieważ Trigger tr_Zlecenia_Czesci_Cena automatycznie pobierze aktualną cenę ze słownika!
INSERT INTO T_Zlecenia_Czesci (ID_Zlecenia, ID_Czesci, Ilosc, Cena_Transakcyjna) VALUES 
(1, 1, 1, 0), -- 1x Klocki hamulcowe do Audi
(1, 2, 1, 0), -- 1x Tarcze hamulcowe do Audi
(2, 3, 1, 0), -- 1x Olej do Toyoty
(2, 4, 1, 0), -- 1x Filtr oleju do Toyoty
(3, 5, 1, 0); -- 1x Zestaw rozrządu do Skody

-- Pozycje Zleceń - Wykonane Usługi
INSERT INTO T_Zlecenia_Uslugi (ID_Zlecenia, ID_Uslugi, ID_Pracownika, Czas_Pracy_Min, Cena_Transakcyjna) VALUES 
(1, 1, 2, 100, 180.00), -- Wymiana klocków i tarcz (Pracownik: Tomasz)
(2, 2, 1, 35, 80.00),   -- Wymiana oleju (Pracownik: Adam)
(2, 3, 1, 20, 150.00),  -- Diagnostyka komputera
(3, 4, 1, 250, 600.00), -- Wymiana rozrządu
(4, 5, 2, 45, 120.00);  -- Przegląd przedsezonowy

PRINT '### Skrypt bazy danych został poprawnie zainicjowany. Dodano rekordy testowe! ###';