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

## Мікросервіс Проєкту №1 — Orders API

Вертикаль реалізована як `Orders.Api → Orders.Bll → Orders.Dal → Orders.Domain`. DAL містить чистий ADO.NET `CustomerRepository`, Dapper `SalesOrderRepository` і `ProductSnapshotRepository` зі спільним `BaseDapperRepository<T>`, ADO.NET `OrderLineRepository` та scoped Unit of Work зі спільними `SqlConnection` і транзакцією. Створення замовлення в BLL однією транзакцією записує заголовок і позиції, беручи назву й ціну з `ProductSnapshot`; статусний перехід користується збереженою процедурою `Order_SetStatus`. Контролери асинхронні й не містять SQL чи бізнес-правил. DTO мають DataAnnotations, мапінг AutoMapper зібраний у BLL, а глобальний обробник віддає `ProblemDetails` для 400/404/409/500.

### Запуск через .NET Aspire

Потрібні .NET 9 SDK та Docker Desktop із запущеним Docker Engine. З кореня репозиторію запустіть:

```powershell
dotnet run --project Platform.AppHost
```

Aspire підніме SQL Server у контейнері з persistent data volume, створить `RpgOrdersDb`, передасть connection string API через ресурс `WithReference` і дочекається готовності БД. При першому старті API через DAL застосує вбудовані `db/p1/schema.sql`, `procedures.sql` і `seed.sql`; скрипти ідемпотентні. Відкрийте посилання Aspire Dashboard у терміналі та перейдіть за адресою `orders-api` → Swagger (`/swagger`). Для повторного запуску дані зберігаються у Docker volume. Щоб повністю скинути локальну базу, видаліть volume `sql-data` через Docker Desktop.

Для запуску API поза Aspire використовується `ConnectionStrings:RpgOrdersDb` у `Services/Orders/Orders.Api/appsettings.json`; змініть її на свій SQL Server. Автоініціалізацію можна вимкнути параметром `Database:InitializeOnStartup=false`, якщо схема вже розгорнута окремо.

### Основні API-запити

Swagger доступний за `/swagger`. Приклади нижче припускають, що API слухає `http://localhost:5000`; фактичний URL Aspire показує в Dashboard.

```powershell
# Список клієнтів і товарів
curl.exe http://localhost:5000/api/customers
curl.exe http://localhost:5000/api/products

# Створити замовлення (CustomerId/ProductId беріть із відповідних GET-відповідей)
curl.exe -X POST http://localhost:5000/api/orders -H "Content-Type: application/json" -d '{"customerId":"11111111-1111-4111-8111-111111111111","lines":[{"productId":"22222222-2222-4222-8222-222222222222","quantity":2}]}'

# Отримати замовлення разом із рядками та RowVersion
curl.exe http://localhost:5000/api/orders/33333333-3333-4333-8333-333333333333

# Змінити статус (RowVersion скопіюйте з відповіді GET)
curl.exe -X PATCH http://localhost:5000/api/orders/33333333-3333-4333-8333-333333333333/status -H "Content-Type: application/json" -d '{"status":"Placed","rowVersion":"BASE64_ROWVERSION"}'

# Пом'якшене видалення; відповідь 204 без тіла
curl.exe -X DELETE http://localhost:5000/api/orders/33333333-3333-4333-8333-333333333333
```

Ендпоінти `api/customers`, `api/products` і `api/orders` підтримують GET/POST/PUT/DELETE; для замовлення PATCH `/api/orders/{id}/status` виконує бізнес-перехід. POST повертає `201 Created` із `Location`, успішні DELETE — `204`. Некоректні DTO повертають `400`, відсутні записи — `404`, повторний email/SKU, конфлікт конкурентної зміни або недозволений статус — `409` у форматі ProblemDetails. Деталі структури проєктів — у [docs/orders-service.md](docs/orders-service.md).

## Catalog BLL та Web API (Проєкт №2)

`Catalog.Api → Catalog.Bll → Catalog.Dal → Catalog.Domain` реалізує API товарів. BLL працює через `ICatalogUnitOfWork`, DTO, AutoMapper і FluentValidation; контролер не звертається до DbContext. Запуск за замовчуванням застосовує EF-міграції й ідемпотентний demo seed. Каталог підтримує SQL-пагінацію, фільтри, дозволене сортування, Specification pattern і структуроване логування Serilog. Swagger UI доступний на `/swagger`. Докладний порядок запуску та запити для перевірки — у [docs/catalog-api.md](docs/catalog-api.md).
