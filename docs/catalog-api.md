# Catalog BLL та Web API

Catalog API має залежності в одному напрямку: `Catalog.Api → Catalog.Bll → Catalog.Dal → Catalog.Domain`. Контролер залежить тільки від `IProductService`. BLL перевіряє SKU та наявність указаного GameSystem, валідує версію конкурентності, застосовує правила сутності та комітить зміни через `ICatalogUnitOfWork`. DTO і AutoMapper profile розміщені в BLL. DataAnnotations працюють під час MVC binding, а FluentValidation validators з BLL assembly викликаються явно в сервісі. Обробник винятків повертає ProblemDetails: 400 для помилок валідації, 404 для невідомого товару або GameSystem, 409 для конфліктів бізнес-правил, дубльованого SKU чи застарілої RowVersion, 500 для неочікуваних помилок.

Під час старту API `CatalogDatabaseInitializer` виконує `MigrateAsync`, потім ідемпотентно додає демонстраційний товар, якщо немає такого ID або SKU. Довідники з фіксованими ID додаються через EF `HasData`. Для керованого зовнішнього розгортання можна вимкнути ініціалізацію в `Services/Catalog/Catalog.Api/appsettings.json`: `Database:InitializeOnStartup=false`.

## Запуск локально у PowerShell

Потрібні .NET 9 SDK, запущений SQL Server і база, створена попередньою міграцією (наприклад `RpgCatalogModelCheckDb`). З кореня `D:\ProjectNET` у VS Code відкрийте **Terminal → New Terminal** та виконайте:

```powershell
$env:CATALOG_DB_CONNECTION = "Server=tcp:127.0.0.1,ВАШ_ПОРТ;Initial Catalog=RpgCatalogModelCheckDb;User ID=sa;Password=ВАШ_SQL_PASSWORD;Encrypt=False;TrustServerCertificate=True"
$env:ASPNETCORE_URLS = "http://localhost:5080"
dotnet run --project Services\Catalog\Catalog.Api\Catalog.Api.csproj
```

Замініть `ВАШ_ПОРТ` на актуальний host port із Docker Desktop (формат відображення `hostPort:1433`) і `ВАШ_SQL_PASSWORD` на пароль SQL Server. Не додавайте `tcp://` у connection string. Залиште це вікно термінала відкритим. У браузері перейдіть на `http://localhost:5080/swagger`.

Якщо змінну `CATALOG_DB_CONNECTION` не задавати, API візьме `ConnectionStrings:CatalogDb` з `Services/Catalog/Catalog.Api/appsettings.json` (Windows Integrated Security на локальному SQL Server).

## Перевірка через Swagger

1. У Swagger розгорніть `GET /api/products` і натисніть **Try it out → Execute**. Очікувана відповідь — `200` і пагінований об'єкт. Після стартового seed там має бути тестовий товар.
   Для перевірки SQL-фільтрів, сортування та сторінок передайте, наприклад, `page=1`, `pageSize=5`, `search=ember`, `productType=Adventure`, `minPrice=1`, `maxPrice=50`, `onlyActive=true`, `sortBy=price`, `descending=true`. Відповідь містить `items`, `totalCount`, `totalPages`, `hasNext`, `hasPrevious`.
2. Розгорніть `POST /api/products` і вставте тіло:

   ```json
   {
     "sku": "SWAGGER-RPG-001",
     "name": "Starter Dice Set",
     "description": "Набір кубиків для настільної рольової гри",
     "productType": "Dice",
     "unitPrice": 12.5,
     "stockQuantity": 20,
     "gameSystemId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"
   }
   ```

   Очікуйте `201 Created`, JSON із `productId` та `rowVersion`, а також заголовок `Location`. Збережіть `productId` і `rowVersion` із відповіді.
3. Виконайте `GET /api/products/{id}` із отриманим `productId`: очікуйте `200` і той самий товар із пов’язаними даними каталогу.
4. Для `PUT /api/products/{id}` використайте `productId` та таке тіло, вставивши `rowVersion` із поточної відповіді GET:

   ```json
   {
     "sku": "SWAGGER-RPG-001",
     "name": "Starter Dice Set — Updated",
     "description": "Оновлений опис",
     "productType": "Dice",
     "unitPrice": 14.0,
     "stockQuantity": 18,
     "gameSystemId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
     "rowVersion": "ВСТАВТЕ_BASE64_ROWVERSION"
   }
   ```

   Очікуйте `200` та оновлену версію. Якщо надіслати стару версію повторно, має бути `409 Conflict` у форматі ProblemDetails.
5. Перевірте валідацію: повторіть POST із тим самим SKU — очікуйте `409`. Надішліть у POST `unitPrice: 0` або порожню назву — MVC DataAnnotations поверне `400` із `ValidationProblemDetails`. Для FluentValidation cross-field правила виконайте GET з `minPrice=50&maxPrice=10`; очікуйте `400` із помилкою `PriceRange`.
6. Перевірте відсутній ID у GET/PUT/DELETE — очікуйте `404` у форматі ProblemDetails. Видаліть тестовий товар через DELETE — очікуйте `204 No Content` без тіла.
7. Перезапустіть API і знову виконайте GET: початковий demo seed не створюється повторно. У терміналі мають з'явитися Serilog request events і EF SQL command logs.

Для компіляційної перевірки з кореня репозиторію виконайте:

```powershell
dotnet build Services\Catalog\Catalog.Api\Catalog.Api.csproj --no-restore -p:NuGetAudit=false -m:1 -nodeReuse:false
```
