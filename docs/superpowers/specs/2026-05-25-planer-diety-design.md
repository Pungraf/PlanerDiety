# Planer Diety - Design

## Cel

Zbudować aplikację Android do planowania diety i tworzenia list zakupów. Aplikacja generuje tygodniowy plan posiłków, pozwala użytkownikowi go przejrzeć i zmodyfikować, a następnie wspiera codzienne korzystanie z planu oraz zakupy na podstawie aktywnego tygodnia.

## Zakres MVP

MVP obejmuje:

- aplikację mobilną na Androida,
- konta użytkowników i synchronizację danych,
- logowanie Google,
- automatyczne generowanie tygodniowego planu posiłków w piątek o 12:00,
- zapis planu jako draft przed zatwierdzeniem,
- ręczną wymianę dowolnego posiłku na dowolny posiłek z bazy,
- kopiowanie całego dnia jadłospisu,
- generowanie tygodniowej listy zakupów w dwóch widokach,
- import bazy przepisów z pliku CSV/Excel realizowany poza aplikacją.

Poza MVP pozostają:

- panel administracyjny,
- ręczna edycja przepisów w aplikacji,
- powiadomienia push,
- dodatkowe role systemowe,
- webowy panel użytkownika.

## Ustalenia produktowe

- Stack ma obsługiwać C#.
- System ma mieć konto użytkownika i synchronizację danych.
- Baza przepisów na start jest ładowana z importu CSV/Excel.
- Po wygenerowaniu plan tygodnia powstaje jako propozycja, którą użytkownik może przejrzeć i ręcznie zmienić przed zatwierdzeniem.
- Na start istnieje jeden typ konta; import realizowany jest technicznie poza aplikacją.
- Logowanie odbywa się przez Google.
- MVP nie zawiera powiadomień.

## Rekomendowana architektura

Rekomendowany stack:

- aplikacja mobilna: .NET MAUI,
- backend: ASP.NET Core Web API,
- baza danych: PostgreSQL,
- harmonogram zadań: proces backendowy uruchamiany cyklicznie po stronie serwera,
- import danych: osobne narzędzie techniczne lub skrypt w C#.

### Uzasadnienie

Ten stack utrzymuje cały projekt w C#, daje pełną kontrolę nad logiką generowania planów, synchronizacją danych i importem przepisów, a jednocześnie nie wymaga kompromisów typowych dla mieszania kilku obcych ekosystemów już na etapie MVP.

## Architektura systemu

System składa się z czterech elementów:

1. **Aplikacja Android w .NET MAUI**
   Odpowiada za logowanie, prezentację planu, wymianę posiłków, kopiowanie dni i listę zakupów.

2. **Backend w ASP.NET Core Web API**
   Udostępnia API dla aplikacji mobilnej, trzyma logikę domenową, synchronizuje dane użytkownika i obsługuje generowanie planów oraz list zakupów.

3. **Baza PostgreSQL**
   Przechowuje użytkowników, posiłki, składniki, tygodniowe plany, statusy zjedzenia i listy zakupów.

4. **Proces harmonogramowany**
   W każdy piątek o 12:00 generuje nowy draft tygodnia dla użytkownika zgodnie z regułami planowania.

## Moduły MVP

### 1. Uwierzytelnianie

Użytkownik loguje się przez Google. Backend po pomyślnym logowaniu tworzy albo aktualizuje konto użytkownika oraz wydaje własny token sesyjny dla aplikacji. Dzięki temu logowanie Google pełni rolę wejścia, ale kontrola nad autoryzacją i danymi pozostaje po stronie systemu.

### 2. Plan tygodniowy

W każdy piątek o 12:00 backend generuje nowy plan tygodnia jako `draft`.

Draft zawiera 7 dni i po 4 posiłki dziennie. Reguły automatycznego generowania:

- śniadanie: wybierane spośród posiłków typu śniadanie, bez deserów,
- drugie śniadanie: wybierane spośród posiłków typu śniadanie, bez deserów,
- obiad: wybierany spośród posiłków typu obiad i ustawiany na dwa następujące po sobie dni,
- kolacja: przed generacją tygodnia użytkownik określa tryb kolacji dla danego tygodnia:
  - kolacja śniadaniowa: wybierana na jeden dzień,
  - kolacja obiadowa: wybierana na dwa następujące po sobie dni.

Po wygenerowaniu użytkownik może przejrzeć draft, ręcznie wymieniać posiłki i dopiero potem zatwierdzić plan. Po zatwierdzeniu plan przechodzi do stanu `active`.

### 3. Dzienny widok posiłków

Ekran główny domyślnie pokazuje bieżący dzień tygodnia. Użytkownik może przełączać dni ręcznie.

Każdy kafelek posiłku pokazuje:

- nazwę posiłku,
- kcal,
- ilość białka,
- status zjedzony / niezjedzony,
- przycisk wymiany posiłku.

Na dole ekranu znajduje się podsumowanie dnia z sumą kcal oraz białka dla wszystkich przypisanych posiłków.

### 4. Wymiana posiłku

Po wejściu w wymianę użytkownik widzi całą bazę potraw. Może filtrować po:

- typie posiłku,
- składniku,
- nazwie posiłku.

Użytkownik może wymienić slot na **dowolny posiłek z bazy**, bez ograniczania do aktualnego typu slotu. Oznacza to, że reguły typów obowiązują przy automatycznym generowaniu planu, ale nie ograniczają późniejszej ręcznej edycji.

W wynikach wyszukiwania pokazujemy co najmniej:

- nazwę posiłku,
- kcal,
- ilość białka.

### 5. Kopiowanie dnia

Użytkownik może skopiować cały jadłospis jednego dnia do innego dnia. Operacja nadpisuje dzień docelowy i po zapisie uruchamia przeliczenie podsumowania dnia oraz listy zakupów dla aktywnego planu.

### 6. Lista zakupów

Lista zakupów jest generowana z aktywnego planu tygodniowego i udostępnia dwa widoki:

- widok sumaryczny z grupowaniem po kategoriach zakupowych,
- widok per potrawa, gdzie pod nazwą dania widnieje lista potrzebnych składników.

Każdy produkt można odklikać. Odkliknięty produkt:

- jest oznaczony jako skreślony,
- trafia na dół odpowiedniej listy,
- może zostać przywrócony przez ponowne kliknięcie.

Lista zakupów jest pochodną aktywnego planu, więc nie jest niezależnie edytowana poza stanem odkliknięcia produktów.

## Przepływ danych

Źródłem prawdy jest backend oraz baza danych.

Aplikacja mobilna pobiera z backendu:

- aktualny plan tygodniowy,
- dane wybranego dnia,
- bazę potraw do filtrowania i wymiany,
- listę zakupów.

Po każdej zmianie użytkownika, takiej jak:

- zatwierdzenie planu,
- wymiana posiłku,
- kopiowanie dnia,
- odkliknięcie produktu,

backend zapisuje zmianę, przelicza pochodne dane i odsyła zaktualizowany stan do aplikacji.

## Model danych

### Główne encje

- `User` - konto użytkownika.
- `Meal` - posiłek z nazwą, typem, flagą deseru, kcal i białkiem.
- `Ingredient` - składnik.
- `MealIngredient` - relacja posiłek-składnik z ilością, jednostką i kategorią zakupową.
- `WeeklyPlan` - plan tygodniowy użytkownika.
- `DailyPlan` - plan dla konkretnego dnia.
- `DailyMealSlot` - slot posiłku w danym dniu, np. śniadanie, drugie śniadanie, obiad, kolacja.
- `ShoppingList` - lista zakupów dla aktywnego tygodnia.
- `ShoppingListItem` - pojedynczy produkt na liście zakupów.

### Reguła domenowa

System musi rozróżniać:

- **slot posiłku** w planie dnia, czyli miejsce typu śniadanie / drugie śniadanie / obiad / kolacja,
- **typ posiłku** w bazie przepisów, np. śniadanie lub obiad.

To rozdzielenie jest konieczne, ponieważ generator używa typu posiłku do budowy draftu, ale użytkownik może później przypisać do slotu dowolny posiłek z bazy.

### Status planu

Statusy wymagane w MVP:

- `draft`,
- `active`.

Status `archived` można dodać później, ale nie jest wymagany w pierwszej wersji.

## Reguły biznesowe

- Nowy draft planu tygodniowego pojawia się w piątek o 12:00.
- Dla jednego dnia automatycznie generowane posiłki nie powinny się powtarzać.
- Obiad w generatorze jest parowany na dwa kolejne dni.
- Tryb kolacji jest wybierany dla całego tygodnia przed wygenerowaniem draftu.
- Ręczna wymiana posiłku może wskazać dowolny przepis z bazy.
- Kopiowanie dnia oznacza pełne nadpisanie dnia docelowego.
- Lista zakupów zawsze wynika z aktywnego planu tygodniowego.

## Obsługa błędów

- Jeśli generator nie znajdzie wystarczającej liczby posiłków do spełnienia reguł, backend nie tworzy niepełnego lub błędnego planu.
- W razie nieudanej generacji backend zapisuje błąd operacyjny i zwraca użytkownikowi komunikat, że baza przepisów wymaga uzupełnienia.
- Po każdej ręcznej zmianie planu backend przelicza dzienne podsumowania i listę zakupów.
- Jeśli zapis zmian się nie powiedzie, aplikacja powinna zachować spójny stan lokalny i pokazać użytkownikowi informację o niepowodzeniu operacji.

## Testy MVP

### Backend

Należy pokryć testami:

- generowanie tygodnia według reguł,
- brak powtórzeń automatycznie generowanych posiłków w obrębie dnia,
- parowanie obiadów na dwa dni,
- logikę kolacji zależnie od wybranego trybu tygodnia,
- zapis draftu i jego aktywację,
- wymianę posiłku na dowolny przepis,
- kopiowanie dnia,
- generowanie listy zakupów w obu widokach,
- zachowanie stanu odkliknięcia produktów,
- ścieżki błędów przy zbyt ubogiej bazie przepisów.

### Aplikacja mobilna

Należy pokryć testami:

- logowanie Google,
- pobranie aktualnego draftu lub aktywnego tygodnia,
- przełączanie dni,
- zatwierdzenie draftu,
- wymianę posiłku,
- kopiowanie dnia,
- odkliknięcie i przywrócenie produktu na liście zakupów,
- prezentację błędu, gdy plan nie może zostać wygenerowany.

## Otwarte decyzje do etapu implementacji

Te kwestie nie blokują designu, ale trzeba je zamknąć w planie wdrożenia:

- wybór konkretnego mechanizmu logowania Google dla .NET MAUI i ASP.NET Core,
- wybór hostingu backendu i bazy danych,
- format importu wejściowego i mapowanie kolumn dla przepisów,
- strategia tokenów i odświeżania sesji,
- zakres ewentualnego cache lokalnego po stronie aplikacji mobilnej.
