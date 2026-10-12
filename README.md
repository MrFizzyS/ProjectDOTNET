# Онлайн-магазин настільних рольових ігор

Навчальна база для майбутніх мікросервісів. Магазин продає книги правил, пригоди, набори кубиків та аксесуари для D&D-подібних ігор. Кожна база є самостійним bounded context; між базами немає зовнішніх ключів, а ідентифікатори інтеграційних посилань зберігаються як звичайні значення.

## Контексти та власники даних

| База | Контекст / сервіс | Сутності, що належать лише цьому контексту |
|---|---|---|
| Проєкт №1 `RpgOrdersDb` (SQL Server, ADO.NET/Dapper) | Оформлення та виконання замовлень | Customer, SalesOrder, OrderLine, OrderPayment, ProductSnapshot, OrderStatusHistory |
| Проєкт №2 `RpgCatalogDb` (SQL Server, EF Core) | Каталог і тематичні підбірки | GameSystem, Product, Category, ProductCategory, Collection, CollectionItem |
| Проєкт №3 `rpg_content` (MongoDB) | Контент і спільнота гравців | Campaign, Session, Review, PlayerProfile |

Замовлення зберігає знімок назви/ціни товару на момент покупки. Каталог є джерелом актуальних описів і цін. Контент MongoDB посилається на ідентифікатори каталогу/замовлення, але не володіє їхніми записами.

## Політика дублювання та узгодження

| Дубльоване поле | Де зберігається | Власник (джерело істини) | Як узгоджується |
|---|---|---|---|
| `ProductId` | p2 `Product.Id`; p1 `OrderLine.ProductId`; p3 `Review.productId` | Каталог (p2) | ID передається подією/запитом; посилання не є FK через межу БД |
| Назва товару | p2 `Product.Name`; p1 `OrderLine.ProductNameSnapshot` | Каталог (p2) | p1 фіксує знімок при оформленні, історичні замовлення не переписуються |
| Ціна товару | p2 `Product.UnitPrice`; p1 `OrderLine.UnitPriceSnapshot` | Каталог (p2) | p1 фіксує ціну при оформленні; зміни каталогу застосовуються лише до нових замовлень |
| `CustomerId` та email | p1 `Customer`; p3 `Review.authorCustomerId` / `PlayerProfile.customerId` | Замовлення (p1) | ID та дозволені публічні дані надходять у p3 подією; профіль може кешувати email, оновлюється подією `CustomerUpdated` |
| Система гри | p2 `GameSystem`; p3 `Campaign.gameSystemId` і `gameSystemName` | Каталог (p2) | ID посилається на каталог, назва в кампанії є знімком і оновлюється подією за потреби |
| `OrderId` і статус | p1 `SalesOrder`; p3 `Review.orderId` (за наявності) | Замовлення (p1) | ID/статус передаються подіями; Mongo review зберігає тільки потрібний знімок статусу придбання |

Події мають містити версію/час зміни; споживачі обробляють повторну доставку ідемпотентно. У навчальних seed-даних UUID між базами узгоджені вручну. Транзакції не перетинають БД.

## Розгортання з нуля

Потрібні SQL Server 2019+ (або сумісний SQL Server) та MongoDB 6+ із `mongosh`. Скрипти SQL створюють власні бази даних і можуть виконуватись повторно. Запускайте файли в наведеному порядку з обліковим записом, що має право створювати БД:

```powershell
sqlcmd -S localhost -E -i db/p1/schema.sql
sqlcmd -S localhost -E -i db/p1/procedures.sql
sqlcmd -S localhost -E -i db/p1/seed.sql
sqlcmd -S localhost -E -i db/p2/schema.sql
sqlcmd -S localhost -E -i db/p2/seed.sql
mongosh "mongodb://localhost:27017" db/p3/collections.js db/p3/seed.js
```

Для SQL-аутентифікації замініть `-E` на `-U <user> -P <password>`. Mongo-скрипти допускають повторне виконання. Повний каталог і приклади документів описані в [docs/architecture.md](docs/architecture.md).

## Короткі ERD

**p1 — замовлення:** `Customer 1 ── N SalesOrder 1 ── N OrderLine`; `SalesOrder 1 ── 0..1 OrderPayment`; `ProductSnapshot 1 ── N OrderLine`; `SalesOrder 1 ── N OrderStatusHistory`. ProductSnapshot тут належить контексту замовлень і є мінімальним локальним snapshot, не FK до каталогу.

**p2 — каталог:** `GameSystem 1 ── N Product`; `Product M ── N Category` через `ProductCategory` (власне поле `AssignedAt`); `GameSystem 1 ── N Collection`; `Collection M ── N Product` через `CollectionItem` (власні `Position`, `AddedAt`).

**p3 — спільнота:** `PlayerProfile` містить вбудовані налаштування/персонажів; `Campaign` містить вбудовані сесії і учасників, а посилається на каталог ID; `Review` посилається на Product/Customer ID, вбудовує оцінки аспектів і довільні відповіді анкети.
