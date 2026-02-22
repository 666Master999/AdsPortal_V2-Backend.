# API Documentation

**Dev HTTP:** `http://localhost:5131/api`  
**Dev HTTPS:** `https://localhost:7145/api`  
**Prod:** `https://adssite.somee.com/api`

## Endpoints

| Method | URL | Auth | Request | Response |
|--------|-----|------|---------|----------|
| POST | `/auth/register` | - | `{ login, password }` | `{ token, user }` |
| POST | `/auth/login` | - | `{ login, password }` | `{ token, user }` |
| GET | `/users/profile` | ✓ | - | `User` |
| PUT | `/users/profile` | ✓ | `{ userName?, email?, phone? }` | `User` |
| POST | `/users/profile/avatar` | ✓ | `FormData(image)` | `{ avatarUrl }` |
| GET | `/users/profiles/{id}` | ✓ | - | `PublicUser` |
| POST | `/ads` | ✓ | `FormData(title, price, type, description?, images)` | `AdDto` |
| GET | `/ads` | - | `?page=1&limit=50` (опционально) | `AdDto[]` |
| GET | `/ads/{id}` | - | - | `AdDto` |

**Auth:** `Authorization: Bearer {token}`  
Токен действует 30 дней, хранится в `localStorage`.

**Пагинация GET /ads:**
- `page` — номер страницы (по умолчанию 1)
- `limit` — количество на странице (по умолчанию 50, макс 100)
- Сортировка: новые первые (по CreatedAt)

---

## TypeScript Interfaces

```typescript
interface AuthResponse {
  token: string
  user: {
    id: number
    login: string
    userName: string
  }
}

interface User {
  id: number
  login: string
  userName: string
  email: string | null
  phone: string | null
  avatarUrl: string | null
  ads: AdDto[]
}

interface PublicUser {
  id: number
  userName: string
  avatarUrl: string | null
  ads: AdDto[]
}

interface AdDto {
  id: number
  title: string
  price: number
  type: 0 | 1 | 2  // 0 = Продам, 1 = Куплю, 2 = Услуги
  isNegotiable: boolean  // Договорная цена
  description: string | null
  imageUrls: string[]
  ownerId: number
  ownerUserName: string
  createdAt: string  // ISO 8601
}

interface AvatarResponse {
  avatarUrl: string  // "/files/{userId}/avatar/av.jpeg"
}
```

---

## Request Examples

### POST /auth/register
```json
{
  "login": "user1",
  "password": "pass123"
}
```

### POST /auth/login
```json
{
  "login": "user1",
  "password": "pass123"
}
```

### PUT /users/profile
```json
{
  "userName": "Иван Иванов",
  "email": "ivan@mail.ru",
  "phone": "+79991234567"
}
```

### POST /users/profile/avatar
```
Content-Type: multipart/form-data

image: [binary file]
```

### POST /ads
```
Content-Type: multipart/form-data

title: "Продам iPhone"               (обязательно, 3-20 символов)
price: 50000                          (обязательно, 0-99999999999999)
type: 0                               (обязательно, 0=Продам, 1=Куплю, 2=Услуги)
isNegotiable: false                   (опционально, по умолчанию false)
description: "Отличное состояние"     (необязательно, макс 3000 символов)
images: [file1, file2, file3]         (необязательно, макс 10 штук)
```

---

## Response Examples

### POST /auth/register, /auth/login
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "login": "user1",
    "userName": "user1"
  }
}
```

### GET /users/profile
```json
{
  "id": 1,
  "login": "user1",
  "userName": "user1",
  "email": null,
  "phone": null,
  "avatarUrl": null,
  "ads": []
}
```

### GET /users/profiles/{id}
```json
{
  "id": 2,
  "userName": "Петр",
  "avatarUrl": "/files/2/avatar/av.jpeg",
  "ads": [
    {
      "id": 5,
      "title": "Продам ноутбук",
      "price": 30000,
      "imageUrls": ["/files/2/userAds/5/1.jpeg"]
    }
  ]
}
```

### POST /users/profile/avatar
```json
{
  "avatarUrl": "/files/1/avatar/av.jpeg"
}
```

### POST /ads
```json
{
  "id": 123,
  "title": "Продам iPhone",
  "price": 50000,
  "type": 0,
  "isNegotiable": false,
  "description": "Отличное состояние",
  "imageUrls": [
    "/files/1/userAds/123/1.jpeg",
    "/files/1/userAds/123/2.jpeg",
    "/files/1/userAds/123/3.jpeg"
  ],
  "ownerId": 1,
  "ownerUserName": "user1",
  "createdAt": "2026-02-22T15:30:00Z"
}
```

**Валидация:**
- `type`: 0 (Продам), 1 (Куплю), 2 (Услуги) — обязательно
- `title`: 3-20 символов — обязательно
- `price`: 0-99999999999999 — обязательно
- `isNegotiable`: true/false — опционально (по умолчанию false)
- `description`: макс 3000 символов — необязательно
- `images`: макс 10 штук — необязательно

**Отображение цены:**
- `isNegotiable: true` → "Договорная"
- `price: 0, isNegotiable: false` → "Бесплатно"
- `price: 50000, isNegotiable: false` → "50 000 ₽"

### GET /ads
```json
[
  {
    "id": 123,
    "title": "Продам iPhone",
    "price": 50000,
    "type": 0,
    "isNegotiable": false,
    "description": "Отличное состояние",
    "imageUrls": ["/files/1/userAds/123/1.jpeg"],
    "ownerId": 1,
    "ownerUserName": "user1",
    "createdAt": "2026-02-22T15:30:00Z"
  },
  {
    "id": 124,
    "title": "Услуги репетитора",
    "price": 0,
    "type": 2,
    "isNegotiable": true,
    "description": "Математика, физика",
    "imageUrls": [],
    "ownerId": 2,
    "ownerUserName": "teacher",
    "createdAt": "2026-02-22T16:00:00Z"
  }
]
```

**Отображение цены:**
```typescript
const displayPrice = (ad: AdDto) => {
  if (ad.isNegotiable) return 'Договорная'
  if (ad.price === 0) return 'Бесплатно'
  return `${ad.price.toLocaleString('ru-RU')} ₽`
}
```

**Типы:**
- `type: 0` → "Продам"
- `type: 1` → "Куплю"
- `type: 2` → "Услуги"

### GET /ads/{id}
```json
{
  "id": 123,
  "title": "Продам iPhone",
  "price": 50000,
  "type": 0,
  "isNegotiable": false,
  "description": "Отличное состояние",
  "imageUrls": [
    "/files/1/userAds/123/1.jpeg",
    "/files/1/userAds/123/2.jpeg"
  ],
  "ownerId": 1,
  "ownerUserName": "user1",
  "createdAt": "2026-02-22T15:30:00Z"
}
```

---

## Error Responses

**401 Unauthorized:**
```json
{ "error": "Unauthorized" }
```

**400 Bad Request:**
```json
{
  "errors": {
    "Login": ["The field Login must be a string with a minimum length of 3"],
    "Password": ["The Password field is required."]
  }
}
```

**404 Not Found:**
```json
{ "error": "Not found" }
```

**500 Internal Server Error:**
```json
{ "error": "Internal server error" }
```

---

## Важно

- Файлы: макс 10MB на запрос
- Изображения сжимаются до 100KB, JPEG
- `userName` при регистрации = `login`, меняется через PUT /users/profile
- Swagger: http://localhost:5131/swagger (dev)

