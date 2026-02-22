# Изменения в проекте

## Структура файлов
```
wwwroot/files/{userId}/
  ├── avatar/av.jpeg
  └── userAds/{adId}/1.jpeg, 2.jpeg...
```

## Модели

**User:**
- `Login` (unique, 50) — для входа
- `UserName` (50) — публичное имя (по умолчанию = Login)
- `Email`, `Phone`, `PasswordHash`, `PasswordSalt`

**Ad:**
- `Title`, `Description`, `Price`, `Type`
- `OwnerId` → Users (cascade delete)

## DTOs

**RegisterDto:**
```csharp
{ Login, Password }  // UserName устанавливается автоматически = Login
```

**UpdateProfileDto:**
```csharp
{ UserName?, Email?, Phone?, Password? }
```

**AdDto:**
```csharp
{ Id, Title, Price, ImageUrls[], OwnerUserName }
```

## Сервисы

**ImageService:**
- `SaveAvatarAsync(file, userId)` → `/files/{userId}/avatar/av.jpeg`
- `SaveAdImagesAsync(files, userId, adId)` → `["/files/{userId}/userAds/{adId}/1.jpeg", ...]`
- Сжатие до 100KB, JPEG, макс 2000x2000px

**UserService:**
- `RegisterAsync` — UserName = Login по умолчанию
- `UpdateProfileAsync` — может изменить UserName

## API

```
POST /api/auth/register
{ "login": "user1", "password": "pass123" }

PUT /api/users/profile
{ "userName": "Иван", "email": "ivan@mail.ru" }

POST /api/users/profile/avatar
image: [file]

POST /api/ads
title, price, type, images: [file1, file2]
→ { "imageUrls": [...] }
```

## База данных

```sql
CREATE TABLE Users (
  Id int PRIMARY KEY,
  Login nvarchar(50) UNIQUE,
  UserName nvarchar(50),
  Email nvarchar(256),
  Phone nvarchar(20),
  PasswordHash varbinary(max),
  PasswordSalt varbinary(max),
  CreatedAt datetime2
);

CREATE TABLE Ads (
  Id int PRIMARY KEY,
  Title nvarchar(max),
  Description nvarchar(max),
  Price decimal(18,2),
  Type int,
  CreatedAt datetime2,
  OwnerId int FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE
);
```

## Команды

```bash
# Пересоздать БД
dotnet ef database drop --force --project AdsPortal_V2
Remove-Item "AdsPortal_V2\Migrations" -Recurse -Force
dotnet ef migrations add InitialCreate --project AdsPortal_V2
dotnet ef database update --project AdsPortal_V2
```

---
**Версия:** .NET 10

## ✅ Финальная структура файлов

### Новая организация:
```
wwwroot/
  └── files/
      └── {userId}/
          ├── avatar/
          │   └── av.jpeg                    (аватар пользователя, макс 100KB)
          └── userAds/
              ├── {adId1}/
              │   ├── 1.jpeg                 (первое изображение объявления)
              │   ├── 2.jpeg                 (второе изображение)
              │   └── 3.jpeg                 (третье изображение)
              └── {adId2}/
                  ├── 1.jpeg
                  └── 2.jpeg
```

**Преимущества:**
- ✅ Все файлы пользователя в одной папке `/files/{userId}/`
- ✅ Поддержка **множественных изображений** для объявлений
- ✅ JPEG формат с адаптивным сжатием до **максимум 100KB**
- ✅ Легко управлять правами доступа
- ✅ Удобная структура для резервного копирования

---

## 🔧 Основные изменения

### 1. **ImageService - полностью переработан**

**Новые методы:**
```csharp
// Аватары
Task<string> SaveAvatarAsync(IFormFile file, int userId);
string GetAvatarPath(int userId);
bool AvatarExists(int userId);
void DeleteAvatar(int userId);

// Объявления (множественные изображения)
Task<List<string>> SaveAdImagesAsync(IFormFileCollection files, int userId, int adId);
List<string> GetAdImagePaths(int userId, int adId);
int GetAdImagesCount(int userId, int adId);
void DeleteAdImages(int userId, int adId);
```

**Алгоритм сжатия:**
1. Ресайз до максимум 2000x2000 (если больше)
2. Сжатие JPEG с качеством 90%
3. Проверка размера → если > 100KB, снизить качество на 10%
4. Повтор п.3 до качества 30%
5. Если всё ещё > 100KB → уменьшить разрешение на 10%
6. Повтор п.5 до достижения 100KB или минимального размера

---

### 2. **Модель Ad - без изменений**
- Нет полей для хранения путей изображений
- Количество изображений и пути вычисляются динамически через `ImageService`

---

### 3. **DTO обновлены**

**AdDto:**
```csharp
public List<string> ImageUrls { get; set; } = new();  // Вместо string? ImageUrl
```

**Пример ответа:**
```json
{
  "id": 123,
  "title": "Продам iPhone",
  "imageUrls": [
    "/files/45/userAds/123/1.jpeg",
    "/files/45/userAds/123/2.jpeg",
    "/files/45/userAds/123/3.jpeg"
  ],
  "ownerUserName": "Иван Иванов"
}
```

---

### 4. **Контроллеры обновлены**

**AdsController - поддержка множественных файлов:**
```csharp
[HttpPost]
public async Task<IActionResult> Create(
    [FromForm] CreateAdDto dto, 
    IFormFileCollection? images)  // ← множественные файлы
{
    // ...
    var imageUrls = await _images.SaveAdImagesAsync(images, ownerId, ad.Id);
    // ...
}
```

**Пример запроса (multipart/form-data):**
```
POST /api/ads
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="title"
Продам iPhone
--boundary
Content-Disposition: form-data; name="images"; filename="photo1.jpg"
[binary data]
--boundary
Content-Disposition: form-data; name="images"; filename="photo2.jpg"
[binary data]
--boundary
```

---

## 📊 Примеры использования

### Загрузка аватара
```bash
curl -X POST https://api.example.com/api/users/profile/avatar \
  -H "Authorization: Bearer {token}" \
  -F "image=@avatar.jpg"
```

**Результат:**
- Файл сохранён: `wwwroot/files/45/avatar/av.jpeg`
- Ответ: `{ "avatarUrl": "/files/45/avatar/av.jpeg" }`

---

### Создание объявления с 3 фото
```bash
curl -X POST https://api.example.com/api/ads \
  -H "Authorization: Bearer {token}" \
  -F "title=Продам iPhone" \
  -F "price=50000" \
  -F "type=0" \
  -F "images=@photo1.jpg" \
  -F "images=@photo2.jpg" \
  -F "images=@photo3.jpg"
```

**Результат:**
- Файлы сохранены:
  - `wwwroot/files/45/userAds/123/1.jpeg`
  - `wwwroot/files/45/userAds/123/2.jpeg`
  - `wwwroot/files/45/userAds/123/3.jpeg`
- Ответ:
```json
{
  "id": 123,
  "imageUrls": [
    "/files/45/userAds/123/1.jpeg",
    "/files/45/userAds/123/2.jpeg",
    "/files/45/userAds/123/3.jpeg"
  ]
}
```

---

### Получение объявления
```bash
curl https://api.example.com/api/ads/123
```

**Ответ:**
```json
{
  "id": 123,
  "title": "Продам iPhone",
  "price": 50000,
  "imageUrls": [
    "/files/45/userAds/123/1.jpeg",
    "/files/45/userAds/123/2.jpeg",
    "/files/45/userAds/123/3.jpeg"
  ],
  "ownerUserName": "Иван Иванов"
}
```

---

## 🔐 Безопасность и ограничения

### Ограничения на размер файлов
В `appsettings.json` или `Program.cs` настройте:
```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB на весь запрос
});
```

### Ограничение количества файлов
В контроллере можно добавить:
```csharp
if (images != null && images.Count > 10)
    return BadRequest("Maximum 10 images allowed");
```

### Валидация типов файлов
`ImageService` автоматически проверяет:
```csharp
if (!file.ContentType.StartsWith("image/"))
    throw new ArgumentException("File is not an image.");
```

---

## 📈 Производительность

### Оптимизация загрузки
- ✅ Все изображения сжимаются до 100KB
- ✅ Асинхронная обработка файлов
- ✅ Динамическое вычисление путей (не хранятся в БД)

### Кеширование
Добавьте в `Program.cs`:
```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.PhysicalPath?.Contains("/files/") == true)
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000"); // 30 дней
        }
    }
});
```

---

## 🛠️ Миграция данных

### Скрипт миграции из старой структуры
```powershell
# Из: wwwroot/files/{userId}/ad/ad.png
# В:   wwwroot/files/{userId}/userAds/{adId}/1.jpeg

$filesRoot = "wwwroot/files"

Get-ChildItem -Path $filesRoot -Directory | ForEach-Object {
    $userId = $_.Name

    # Пропускаем служебные папки
    if ($userId -notmatch '^\d+$') { return }

    # Миграция аватара
    $oldAvatar = Join-Path $_.FullName "avatar/avatar.png"
    if (Test-Path $oldAvatar) {
        $newAvatarDir = Join-Path $_.FullName "avatar"
        $newAvatar = Join-Path $newAvatarDir "av.jpeg"

        # Конвертируем PNG → JPEG
        # (требуется ImageMagick или скрипт на C#)
        Copy-Item $oldAvatar $newAvatar
        Write-Host "Migrated avatar for user $userId"
    }

    # Миграция объявлений
    $adsFolder = Join-Path $_.FullName "ad"
    if (Test-Path $adsFolder) {
        # Запросить из БД список объявлений пользователя
        # Для каждого объявления создать папку userAds/{adId}/
        # Переместить файлы
    }
}
```

---

## 📝 Обновление фронтенда

### Загрузка множественных файлов (React)
```jsx
const handleUpload = async (adData, files) => {
  const formData = new FormData();
  formData.append('title', adData.title);
  formData.append('price', adData.price);
  formData.append('type', adData.type);

  // Добавляем все файлы
  for (const file of files) {
    formData.append('images', file);
  }

  const response = await fetch('/api/ads', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });

  const result = await response.json();
  console.log('Image URLs:', result.imageUrls);
};
```

### Отображение галереи
```jsx
const AdGallery = ({ ad }) => (
  <div className="gallery">
    {ad.imageUrls.map((url, index) => (
      <img key={index} src={url} alt={`${ad.title} ${index + 1}`} />
    ))}
  </div>
);
```

---

## 🎯 Следующие шаги

1. ✅ **Применить изменения** - код готов, проект собирается
2. ⏳ **Тестирование:**
   - Загрузка аватара
   - Создание объявления с 1-5 изображениями
   - Проверка размера файлов (должно быть ≤ 100KB)
   - Проверка путей в ответах API
3. ⏳ **Обновить фронтенд:**
   - Изменить `imageUrl` → `imageUrls` (массив)
   - Добавить галерею для множественных фото
   - Поддержка загрузки нескольких файлов
4. ⏳ **Документация API:**
   - Обновить Swagger описание
   - Примеры запросов с множественными файлами

---

## 📦 Итоговые файлы

| Компонент | Статус | Описание |
|-----------|--------|----------|
| `Models/User.cs` | ✅ Обновлено | Добавлено `UserName` |
| `Models/Ad.cs` | ✅ Без изменений | Нет полей для изображений |
| `Services/IImageService.cs` | ✅ Полностью переработано | Новые методы для аватаров и галерей |
| `Services/ImageService.cs` | ✅ Полностью переработано | JPEG, 100KB, множественные файлы |
| `DTOs/AdDto.cs` | ✅ Обновлено | `ImageUrls` вместо `ImageUrl` |
| `DTOs/RegisterDto.cs` | ✅ Обновлено | Добавлено `UserName` |
| `DTOs/UpdateProfileDto.cs` | ✅ Обновлено | Добавлено `UserName` |
| `Controllers/AdsController.cs` | ✅ Обновлено | `IFormFileCollection images` |
| `Controllers/UsersController.cs` | ✅ Обновлено | Работа с `ImageUrls` |

---

## 🔍 Проверка структуры

После создания объявления с фото проверьте:
```bash
tree wwwroot/files/45
```

**Ожидаемый результат:**
```
wwwroot/files/45/
├── avatar/
│   └── av.jpeg
└── userAds/
    ├── 123/
    │   ├── 1.jpeg
    │   ├── 2.jpeg
    │   └── 3.jpeg
    └── 124/
        └── 1.jpeg
```

---

**Автор:** GitHub Copilot  
**Дата:** 22.02.2026  
**Версия:** .NET 10, C# 14  
**Структура:** `wwwroot/files/{userId}/avatar/av.jpeg` + `wwwroot/files/{userId}/userAds/{adId}/1.jpeg`

