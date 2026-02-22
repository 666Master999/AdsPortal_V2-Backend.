# Инструкция по мультизагрузке изображений

## API

**Endpoint:** `POST http://localhost:5131/api/ads`  
**Auth:** `Authorization: Bearer {token}`  
**Content-Type:** `multipart/form-data`

**Request:**
```
title: "Продам iPhone"      (обязательно, 3-20 символов)
price: 50000                 (обязательно, 0-99999999999999, 0=бесплатно/договорная)
type: 0                      (обязательно, 0=Продам, 1=Куплю, 2=Услуги)
description: "..."           (необязательно, макс 3000 символов)
images: [file1, file2, ...]  (необязательно, макс 10 штук, ПОРЯДОК ВАЖЕН!)
```

**Response:**
```json
{
  "id": 123,
  "type": 0,
  "title": "Продам iPhone",
  "price": 50000,
  "description": "...",
  "imageUrls": [
    "/files/1/userAds/123/1.jpeg",  ← ГЛАВНОЕ (первое)
    "/files/1/userAds/123/2.jpeg",
    "/files/1/userAds/123/3.jpeg"
  ]
}public enum AdType
{
    Sell = 0,      // Продам
    Buy = 1,       // Куплю
    Services = 2   // Услуги
}
```

---

## Валидация полей

```typescript
interface CreateAdForm {
  type: 0 | 1 | 2          // ОБЯЗАТЕЛЬНО (0=Продам, 1=Куплю, 2=Услуги)
  title: string            // ОБЯЗАТЕЛЬНО (3-20 символов)
  price: number            // ОБЯЗАТЕЛЬНО (0-99999999999999, 0=бесплатно/договорная)
  description?: string     // НЕОБЯЗАТЕЛЬНО (макс 3000 символов)
  images: File[]           // НЕОБЯЗАТЕЛЬНО (макс 10 штук)
}
```

**Проверки:**
```typescript
// Title
if (title.length < 3 || title.length > 20) {
  alert('Заголовок: 3-20 символов')
}

// Price
if (price < 0 || price > 99999999999999) {
  alert('Цена: 0-99999999999999')
}

// Description
if (description && description.length > 3000) {
  alert('Описание: макс 3000 символов')
}

// Images
if (images.length > 10) {
  alert('Максимум 10 изображений')
}
```

**Отображение цены:**
```typescript
const displayPrice = (price: number) => {
  if (price === 0) return 'Бесплатно / Договорная'
  return `${price.toLocaleString('ru-RU')} ₽`
}
```

---

## Логика изображений

### 1. Хранение

```typescript
const images = ref<File[]>([])
const mainImageIndex = ref(0)  // Индекс главного (0 = первое)
```

### 2. Добавление

```typescript
const handleFileSelect = (files: FileList) => {
  const fileArray = Array.from(files)

  if (images.value.length + fileArray.length > 10) {
    alert('Максимум 10 изображений')
    return
  }

  const imageFiles = fileArray.filter(f => f.type.startsWith('image/'))
  images.value.push(...imageFiles)
}
```

### 3. Удаление

```typescript
const removeImage = (index: number) => {
  images.value.splice(index, 1)

  if (mainImageIndex.value === index) {
    mainImageIndex.value = 0
  } else if (mainImageIndex.value > index) {
    mainImageIndex.value--
  }
}
```

### 4. Установить главным

```typescript
const setMainImage = (index: number) => {
  mainImageIndex.value = index
}
```

### 5. Изменение порядка

```typescript
const moveUp = (index: number) => {
  if (index === 0) return

  [images.value[index], images.value[index - 1]] = 
    [images.value[index - 1], images.value[index]]

  if (mainImageIndex.value === index) {
    mainImageIndex.value--
  } else if (mainImageIndex.value === index - 1) {
    mainImageIndex.value++
  }
}

const moveDown = (index: number) => {
  if (index === images.value.length - 1) return

  [images.value[index], images.value[index + 1]] = 
    [images.value[index + 1], images.value[index]]

  if (mainImageIndex.value === index) {
    mainImageIndex.value++
  } else if (mainImageIndex.value === index + 1) {
    mainImageIndex.value--
  }
}
```

### 6. Отправка

```typescript
const submit = async () => {
  // Валидация
  if (form.title.length < 3 || form.title.length > 20) {
    alert('Заголовок: 3-20 символов')
    return
  }

  // ВАЖНО: главное изображение должно быть первым!
  const reordered = [...images.value]
  if (mainImageIndex.value > 0) {
    const main = reordered.splice(mainImageIndex.value, 1)[0]
    reordered.unshift(main)
  }

  const formData = new FormData()
  formData.append('type', form.type.toString())
  formData.append('title', form.title)
  formData.append('price', form.price.toString())
  if (form.description) formData.append('description', form.description)

  // Порядок важен!
  reordered.forEach(img => formData.append('images', img))

  const res = await fetch('http://localhost:5131/api/ads', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  })

  const ad = await res.json()
  console.log('Создано:', ad)
}
```

---

## Важно

1. **Первое изображение = главное**  
   Бэкенд автоматически установит `isMain = true` для первого файла в массиве.

2. **Перед отправкой:**  
   Переставь главное изображение на первое место в массиве.

3. **Типы объявлений:**
   - `0` = Продам
   - `1` = Куплю
   - `2` = Услуги

4. **Цена 0:**  
   На фронте отображай как "Бесплатно" или "Договорная"

5. **Ограничения:**
   - Заголовок: 3-20 символов
   - Описание: макс 3000 символов
   - Цена: 0-99999999999999
   - Изображения: макс 10 штук
   - Сервер сжимает изображения до 100KB, JPEG

6. **Отображение:**
   ```typescript
   const mainImage = ad.imageUrls[0]  // Всегда первое
   ```

---

## Структура БД

```sql
Ads:
  - Type (0=Продам, 1=Куплю, 2=Услуги)
  - Title (3-20 символов)
  - Description (макс 3000)
  - Price (0-99999999999999, 0=бесплатно/договорная)

AdImages:
  - AdId (FK → Ads, cascade delete)
  - FilePath ("/files/1/userAds/123/1.jpeg")
  - IsMain (bool) ← первое = true
  - Order (int)   ← порядок
```

**Готово.**
