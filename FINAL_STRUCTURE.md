# Структура AdsPortal_V2

## Файлы
```
wwwroot/files/{userId}/
  ├── avatar/av.jpeg              (макс 100KB)
  └── userAds/{adId}/1.jpeg, 2.jpeg...
```

## API

### Регистрация
```
POST /api/auth/register
{ "login": "user1", "password": "pass123" }
→ { "token": "..." }
```

### Загрузка аватара
```
POST /api/users/profile/avatar
Authorization: Bearer {token}
Content-Type: multipart/form-data
image: [file]
→ { "avatarUrl": "/files/45/avatar/av.jpeg" }
```

### Создание объявления
```
POST /api/ads
Authorization: Bearer {token}
title: "iPhone"
price: 50000
type: 0
images: [file1, file2, file3]
→ {
  "id": 123,
  "imageUrls": ["/files/45/userAds/123/1.jpeg", "2.jpeg", "3.jpeg"],
  "ownerUserName": "user1"
}
```

### Обновление профиля
```
PUT /api/users/profile
Authorization: Bearer {token}
{
  "userName": "Иван Иванов",
  "email": "ivan@mail.ru",
  "phone": "+79991234567"
}
```

## База данных

### Users
- `Login` (unique) — для входа
- `UserName` — публичное имя (по умолчанию = Login)
- `Email`, `Phone`, `PasswordHash`, `PasswordSalt`

### Ads
- `Title`, `Description`, `Price`, `Type`
- `OwnerId` → Users (cascade delete)

## Frontend (Vue 3 + Bootstrap)

### Composable
```typescript
// composables/useApi.ts
const uploadAvatar = async (file: File) => {
  const formData = new FormData()
  formData.append('image', file)
  const res = await fetch('/api/users/profile/avatar', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  })
  return (await res.json()).avatarUrl
}

const createAd = async (data, images: File[]) => {
  const formData = new FormData()
  formData.append('title', data.title)
  formData.append('price', data.price)
  formData.append('type', data.type)
  images.forEach(img => formData.append('images', img))

  const res = await fetch('/api/ads', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  })
  return await res.json()
}
```

### Компонент загрузки аватара
```vue
<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '@/composables/useApi'
import { useUserStore } from '@/stores/user'

const { uploadAvatar } = useApi()
const userStore = useUserStore()
const uploading = ref(false)

const handleFileSelect = async (e: Event) => {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return

  uploading.value = true
  try {
    const url = await uploadAvatar(file)
    userStore.updateAvatar(url)
  } finally {
    uploading.value = false
  }
}
</script>

<template>
  <div class="text-center">
    <img v-if="userStore.avatarUrl" :src="userStore.avatarUrl" class="rounded-circle" style="width:120px;height:120px;object-fit:cover">
    <div v-else class="rounded-circle bg-secondary d-flex align-items-center justify-content-center" style="width:120px;height:120px">
      {{ userStore.userName?.[0].toUpperCase() }}
    </div>
    <input type="file" @change="handleFileSelect" accept="image/*" class="d-none" ref="fileInput">
    <button @click="$refs.fileInput.click()" :disabled="uploading" class="btn btn-primary mt-3">
      {{ uploading ? 'Загрузка...' : 'Загрузить аватар' }}
    </button>
  </div>
</template>
```

### Галерея (Bootstrap Carousel)
```vue
<template>
  <div id="carousel" class="carousel slide">
    <div class="carousel-inner rounded">
      <div v-for="(url, i) in imageUrls" :key="i" :class="['carousel-item', {active: i===0}]">
        <img :src="url" class="d-block w-100" style="aspect-ratio:4/3;object-fit:contain;background:#f0f0f0">
      </div>
    </div>
    <button class="carousel-control-prev" data-bs-target="#carousel" data-bs-slide="prev">
      <span class="carousel-control-prev-icon"></span>
    </button>
    <button class="carousel-control-next" data-bs-target="#carousel" data-bs-slide="next">
      <span class="carousel-control-next-icon"></span>
    </button>
  </div>
</template>
```

## Pinia Stores

```typescript
// stores/user.ts
export const useUserStore = defineStore('user', () => {
  const id = ref<number | null>(null)
  const userName = ref<string | null>(null)
  const avatarUrl = ref<string | null>(null)

  const setUser = (data: any) => {
    id.value = data.id
    userName.value = data.userName
    avatarUrl.value = data.avatarUrl
  }

  const updateAvatar = (url: string) => {
    avatarUrl.value = url
  }

  return { id, userName, avatarUrl, setUser, updateAvatar }
})

// stores/auth.ts
export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token'))

  const setToken = (t: string) => {
    token.value = t
    localStorage.setItem('token', t)
  }

  return { token, setToken }
})
```

## Команды

```bash
# Пересоздать БД
dotnet ef database drop --force --project AdsPortal_V2
Remove-Item "AdsPortal_V2\Migrations" -Recurse -Force
dotnet ef migrations add InitialCreate --project AdsPortal_V2
dotnet ef database update --project AdsPortal_V2

# Запуск
dotnet run --project AdsPortal_V2
```

---

**Версия:** .NET 10, EF Core 10, Vue 3, Bootstrap 5

### Vue 3 - Composable для работы с API
```typescript
// composables/useApi.ts
import { useAuthStore } from '@/stores/auth'

export const useApi = () => {
  const authStore = useAuthStore()

  const uploadAvatar = async (file: File) => {
    const formData = new FormData()
    formData.append('image', file)

    const response = await fetch('/api/users/profile/avatar', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authStore.token}`
      },
      body: formData
    })

    if (!response.ok) {
      throw new Error('Failed to upload avatar')
    }

    const { avatarUrl } = await response.json()
    return avatarUrl
  }

  const createAd = async (adData: any, images: File[]) => {
    const formData = new FormData()
    formData.append('title', adData.title)
    formData.append('price', adData.price.toString())
    formData.append('type', adData.type.toString())
    if (adData.description) {
      formData.append('description', adData.description)
    }

    // Добавляем все изображения
    images.forEach(image => {
      formData.append('images', image)
    })

    const response = await fetch('/api/ads', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authStore.token}`
      },
      body: formData
    })

    if (!response.ok) {
      throw new Error('Failed to create ad')
    }

    return await response.json()
  }

  return {
    uploadAvatar,
    createAd
  }
}
```

### Vue 3 - Компонент загрузки аватара (Bootstrap)
```vue
<!-- components/AvatarUpload.vue -->
<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '@/composables/useApi'
import { useUserStore } from '@/stores/user'

const { uploadAvatar } = useApi()
const userStore = useUserStore()

const uploading = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)

const handleFileSelect = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]

  if (!file) return

  if (!file.type.startsWith('image/')) {
    alert('Пожалуйста, выберите изображение')
    return
  }

  uploading.value = true
  try {
    const avatarUrl = await uploadAvatar(file)
    userStore.updateAvatar(avatarUrl)
    alert('Аватар успешно загружен!')
  } catch (error) {
    console.error('Ошибка загрузки аватара:', error)
    alert('Не удалось загрузить аватар')
  } finally {
    uploading.value = false
  }
}

const triggerFileInput = () => {
  fileInput.value?.click()
}
</script>

<template>
  <div class="text-center">
    <input
      ref="fileInput"
      type="file"
      accept="image/*"
      @change="handleFileSelect"
      class="d-none"
    />

    <!-- Avatar Preview -->
    <div class="d-inline-block position-relative mb-3">
      <img
        v-if="userStore.avatarUrl"
        :src="userStore.avatarUrl"
        alt="Avatar"
        class="rounded-circle"
        style="width: 120px; height: 120px; object-fit: cover;"
      />
      <div
        v-else
        class="rounded-circle bg-secondary d-flex align-items-center justify-content-center text-white"
        style="width: 120px; height: 120px; font-size: 48px;"
      >
        {{ userStore.userName?.charAt(0).toUpperCase() }}
      </div>
    </div>

    <div>
      <button
        @click="triggerFileInput"
        :disabled="uploading"
        class="btn btn-primary"
      >
        <span v-if="uploading" class="spinner-border spinner-border-sm me-2"></span>
        {{ uploading ? 'Загрузка...' : 'Загрузить аватар' }}
      </button>
    </div>
  </div>
</template>
```

### Vue 3 - Форма создания объявления (Bootstrap)
```vue
<!-- components/CreateAdForm.vue -->
<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '@/composables/useApi'
import { useRouter } from 'vue-router'

const { createAd } = useApi()
const router = useRouter()

const form = ref({
  title: '',
  price: 0,
  type: 0,
  description: ''
})

const selectedFiles = ref<File[]>([])
const previewUrls = ref<string[]>([])
const submitting = ref(false)

const handleFilesSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  const files = Array.from(target.files || [])

  // Ограничение до 10 фото
  if (selectedFiles.value.length + files.length > 10) {
    alert('Максимум 10 изображений')
    return
  }

  // Проверка типов файлов
  const imageFiles = files.filter(f => f.type.startsWith('image/'))

  if (imageFiles.length !== files.length) {
    alert('Некоторые файлы не являются изображениями')
  }

  selectedFiles.value.push(...imageFiles)

  // Создаем превью
  imageFiles.forEach(file => {
    const reader = new FileReader()
    reader.onload = (e) => {
      previewUrls.value.push(e.target?.result as string)
    }
    reader.readAsDataURL(file)
  })
}

const removeImage = (index: number) => {
  selectedFiles.value.splice(index, 1)
  previewUrls.value.splice(index, 1)
}

const handleSubmit = async () => {
  if (!form.value.title || form.value.price <= 0) {
    alert('Заполните все обязательные поля')
    return
  }

  submitting.value = true
  try {
    const ad = await createAd(form.value, selectedFiles.value)
    alert('Объявление создано!')
    router.push(`/ads/${ad.id}`)
  } catch (error) {
    console.error('Ошибка создания объявления:', error)
    alert('Не удалось создать объявление')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="container py-4">
    <div class="row justify-content-center">
      <div class="col-md-8 col-lg-6">
        <div class="card shadow-sm">
          <div class="card-body">
            <h2 class="card-title mb-4">Создать объявление</h2>

            <form @submit.prevent="handleSubmit">
              <!-- Тип объявления -->
              <div class="mb-3">
                <label class="form-label">Тип объявления</label>
                <select v-model.number="form.type" class="form-select">
                  <option :value="0">Продажа</option>
                  <option :value="1">Покупка</option>
                </select>
              </div>

              <!-- Название -->
              <div class="mb-3">
                <label class="form-label">Название <span class="text-danger">*</span></label>
                <input
                  v-model="form.title"
                  type="text"
                  class="form-control"
                  required
                  placeholder="Например: Продам iPhone 15 Pro"
                />
              </div>

              <!-- Цена -->
              <div class="mb-3">
                <label class="form-label">Цена <span class="text-danger">*</span></label>
                <div class="input-group">
                  <input
                    v-model.number="form.price"
                    type="number"
                    min="0"
                    class="form-control"
                    required
                    placeholder="0"
                  />
                  <span class="input-group-text">₽</span>
                </div>
              </div>

              <!-- Описание -->
              <div class="mb-3">
                <label class="form-label">Описание</label>
                <textarea
                  v-model="form.description"
                  rows="4"
                  class="form-control"
                  placeholder="Расскажите подробнее о товаре..."
                ></textarea>
              </div>

              <!-- Фотографии -->
              <div class="mb-4">
                <label class="form-label">
                  Фотографии
                  <span class="badge bg-secondary ms-2">
                    {{ selectedFiles.length }}/10
                  </span>
                </label>

                <input
                  type="file"
                  multiple
                  accept="image/*"
                  @change="handleFilesSelect"
                  :disabled="selectedFiles.length >= 10"
                  class="form-control"
                />

                <!-- Превью изображений -->
                <div v-if="previewUrls.length" class="row g-2 mt-2">
                  <div
                    v-for="(url, index) in previewUrls"
                    :key="index"
                    class="col-4 col-md-3"
                  >
                    <div class="position-relative">
                      <img
                        :src="url"
                        :alt="`Фото ${index + 1}`"
                        class="img-thumbnail w-100"
                        style="aspect-ratio: 1; object-fit: cover;"
                      />
                      <button
                        type="button"
                        @click="removeImage(index)"
                        class="btn btn-danger btn-sm position-absolute top-0 end-0 m-1 rounded-circle p-0"
                        style="width: 24px; height: 24px;"
                      >
                        ✕
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Кнопка отправки -->
              <button
                type="submit"
                :disabled="submitting || !form.title || form.price <= 0"
                class="btn btn-primary w-100"
              >
                <span v-if="submitting" class="spinner-border spinner-border-sm me-2"></span>
                {{ submitting ? 'Создание...' : 'Создать объявление' }}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
```

### Vue 3 - Галерея изображений (Bootstrap Carousel)
```vue
<!-- components/AdGallery.vue -->
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'

interface Props {
  imageUrls: string[]
  title: string
}

const props = defineProps<Props>()

const carouselEl = ref<HTMLElement | null>(null)
const selectedImageIndex = ref(0)

const currentImage = computed(() => props.imageUrls[selectedImageIndex.value])

const selectImage = (index: number) => {
  selectedImageIndex.value = index
  // Bootstrap carousel API
  const carousel = carouselEl.value
  if (carousel && (window as any).bootstrap) {
    const bsCarousel = (window as any).bootstrap.Carousel.getInstance(carousel) ||
                       new (window as any).bootstrap.Carousel(carousel)
    bsCarousel.to(index)
  }
}

onMounted(() => {
  // Слушаем события Bootstrap carousel
  const carousel = carouselEl.value
  if (carousel) {
    carousel.addEventListener('slid.bs.carousel', (event: any) => {
      selectedImageIndex.value = event.to
    })
  }
})
</script>

<template>
  <div class="ad-gallery">
    <div v-if="imageUrls.length > 0">
      <!-- Bootstrap Carousel -->
      <div
        ref="carouselEl"
        id="adImagesCarousel"
        class="carousel slide mb-3"
        data-bs-ride="carousel"
      >
        <!-- Indicators -->
        <div v-if="imageUrls.length > 1" class="carousel-indicators">
          <button
            v-for="(url, index) in imageUrls"
            :key="index"
            type="button"
            data-bs-target="#adImagesCarousel"
            :data-bs-slide-to="index"
            :class="{ active: index === 0 }"
            :aria-current="index === 0 ? 'true' : 'false'"
            :aria-label="`Slide ${index + 1}`"
          ></button>
        </div>

        <!-- Slides -->
        <div class="carousel-inner rounded">
          <div
            v-for="(url, index) in imageUrls"
            :key="index"
            :class="['carousel-item', { active: index === 0 }]"
          >
            <img
              :src="url"
              :alt="`${title} - фото ${index + 1}`"
              class="d-block w-100"
              style="aspect-ratio: 4/3; object-fit: contain; background: #f0f0f0;"
            />
          </div>
        </div>

        <!-- Controls -->
        <button
          v-if="imageUrls.length > 1"
          class="carousel-control-prev"
          type="button"
          data-bs-target="#adImagesCarousel"
          data-bs-slide="prev"
        >
          <span class="carousel-control-prev-icon" aria-hidden="true"></span>
          <span class="visually-hidden">Предыдущее</span>
        </button>
        <button
          v-if="imageUrls.length > 1"
          class="carousel-control-next"
          type="button"
          data-bs-target="#adImagesCarousel"
          data-bs-slide="next"
        >
          <span class="carousel-control-next-icon" aria-hidden="true"></span>
          <span class="visually-hidden">Следующее</span>
        </button>
      </div>

      <!-- Thumbnails -->
      <div v-if="imageUrls.length > 1" class="row g-2">
        <div
          v-for="(url, index) in imageUrls"
          :key="index"
          class="col-3 col-md-2"
        >
          <img
            :src="url"
            :alt="`${title} - миниатюра ${index + 1}`"
            @click="selectImage(index)"
            :class="[
              'img-thumbnail w-100',
              { 'border-primary border-3': index === selectedImageIndex }
            ]"
            style="aspect-ratio: 1; object-fit: cover; cursor: pointer;"
          />
        </div>
      </div>
    </div>

    <!-- Нет изображений -->
    <div v-else class="text-center p-5 bg-light rounded">
      <i class="bi bi-image fs-1 text-muted"></i>
      <p class="text-muted mt-2">Нет фотографий</p>
    </div>
  </div>
</template>
```

### Pinia Store - User Store
```typescript
// stores/user.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useUserStore = defineStore('user', () => {
  const id = ref<number | null>(null)
  const login = ref<string | null>(null)
  const userName = ref<string | null>(null)
  const email = ref<string | null>(null)
  const avatarUrl = ref<string | null>(null)
  const ads = ref<any[]>([])

  const isAuthenticated = computed(() => id.value !== null)

  const setUser = (userData: any) => {
    id.value = userData.id
    login.value = userData.login
    userName.value = userData.userName
    email.value = userData.email
    avatarUrl.value = userData.avatarUrl
    ads.value = userData.ads || []
  }

  const updateAvatar = (url: string) => {
    avatarUrl.value = url
  }

  const clearUser = () => {
    id.value = null
    login.value = null
    userName.value = null
    email.value = null
    avatarUrl.value = null
    ads.value = []
  }

  return {
    id,
    login,
    userName,
    email,
    avatarUrl,
    ads,
    isAuthenticated,
    setUser,
    updateAvatar,
    clearUser
  }
})
```

### Pinia Store - Auth Store
```typescript
// stores/auth.ts
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))

  const setToken = (newToken: string) => {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  const clearToken = () => {
    token.value = null
    localStorage.removeItem('token')
  }

  return {
    token,
    setToken,
    clearToken
  }
})
```

### Подключение Bootstrap (main.ts)
```typescript
// main.ts
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'

// Bootstrap CSS
import 'bootstrap/dist/css/bootstrap.min.css'
// Bootstrap Icons (опционально)
import 'bootstrap-icons/font/bootstrap-icons.css'

// Bootstrap JS (для carousel, modals и т.д.)
import 'bootstrap/dist/js/bootstrap.bundle.min.js'

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')
```

---

## 📋 Чеклист для тестирования

- [ ] Регистрация нового пользователя с `userName`
- [ ] Загрузка аватара → проверить путь `/files/{userId}/avatar/av.jpeg`
- [ ] Создание объявления без фото
- [ ] Создание объявления с 1 фото
- [ ] Создание объявления с 5 фото
- [ ] Проверка размера всех изображений ≤ 100KB
- [ ] Получение объявления → массив `imageUrls`
- [ ] Профиль пользователя → показывает `userName`, а не `login` (для чужого профиля)
- [ ] Удаление объявления → папка `/files/{userId}/userAds/{adId}/` удаляется

---

## 🛠️ Команды

### Применить миграцию
```bash
dotnet ef database update --project AdsPortal_V2
```

### Запустить проект
```bash
dotnet run --project AdsPortal_V2
```

### Проверить структуру файлов
```bash
# Windows
tree wwwroot\files

# Linux/Mac
tree wwwroot/files
```

### Проверить размеры изображений
```bash
# Windows PowerShell
Get-ChildItem -Path "wwwroot\files" -Recurse -Filter "*.jpeg" | 
  ForEach-Object { 
    [PSCustomObject]@{
      Name = $_.FullName
      SizeKB = [Math]::Round($_.Length / 1KB, 2)
    }
  } | Format-Table -AutoSize
```

---

## 📌 Важные замечания

1. **Login vs UserName:**
   - `Login` — используется только для входа (приватный)
   - `UserName` — отображается в объявлениях и профилях (публичный)

2. **Множественные изображения:**
   - Нумерация начинается с `1.jpeg`
   - Порядок сохранения = порядок в запросе
   - При получении сортируются по номеру

3. **Сжатие изображений:**
   - Гарантированно ≤ 100KB
   - Качество снижается постепенно (90% → 80% → ... → 30%)
   - Если не помогает — уменьшается разрешение

4. **Безопасность:**
   - Проверка типа файла: `image/*`
   - Ограничение размера запроса: 10MB (настраивается)
   - Рекомендуется добавить антивирусную проверку для продакшена

---

## 🎯 Roadmap (будущие улучшения)

1. **Thumbnails (миниатюры):**
   - Создавать `/files/{userId}/userAds/{adId}/thumb_1.jpeg` (200x200)
   - Использовать в списках объявлений

2. **Ограничение количества фото:**
   - Максимум 10 изображений на объявление

3. **Обработка ошибок:**
   - Если загрузка фото не удалась — откатить создание объявления

4. **Фоновая очистка:**
   - Удалять папки объявлений через N дней после удаления записи из БД

5. **CDN интеграция:**
   - При росте нагрузки подключить Cloudflare или Azure CDN

6. **Watermark (водяной знак):**
   - Добавлять логотип сайта на изображения

---

**Версия:** 1.0  
**Дата:** 22.02.2026  
**Технологии:** ASP.NET Core 10, EF Core, SixLabors.ImageSharp 3.1.12
