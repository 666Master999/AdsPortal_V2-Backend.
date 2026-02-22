# Пересоздание БД — Отчёт

**Дата:** 22.02.2026

## Выполнено

1. **Удалена** старая база `AdsPortalV2`
2. **Очищены** все миграции
3. **Создана** миграция `InitialCreate`
4. **Применена** к новой БД

## Структура

**Users:**
- Login (nvarchar(50), unique) — для входа
- UserName (nvarchar(50)) — публичное имя, по умолчанию = Login
- Email, Phone, PasswordHash, PasswordSalt, CreatedAt

**Ads:**
- Title, Description, Price, Type, CreatedAt
- OwnerId → Users (cascade delete)

## Файлы

```
wwwroot/files/{userId}/
  ├── avatar/av.jpeg
  └── userAds/{adId}/1.jpeg, 2.jpeg...
```

## Проверка

```sql
-- Структура Users
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;

-- Структура Ads
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Ads'
ORDER BY ORDINAL_POSITION;
```

## Тестирование

```bash
dotnet run --project AdsPortal_V2
# Swagger: http://localhost:5000/swagger

# Регистрация
POST /api/auth/register
{ "login": "admin", "password": "Admin123!" }

# Обновить профиль
PUT /api/users/profile
{ "userName": "Администратор", "email": "admin@mail.ru" }
```

---
**Статус:** ✅ БД готова
