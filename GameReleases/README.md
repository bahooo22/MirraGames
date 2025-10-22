# Game Releases Backend Service

Backend-сервис для сбора и анализа данных о будущих релизах игр из Steam.

## 🚀 Функциональность

- Синхронизация данных о играх из Steam API
- Календарь релизов на ноябрь 2025
- Аналитика по жанрам (топ-5 популярных тегов)
- Динамика изменений за последние 3 месяца
- REST API для интеграции с фронтендом

## 🛠 Технологии

- ASP.NET Core 8
- Entity Framework Core + PostgreSQL
- Docker + Docker Compose
- Swagger/OpenAPI
- HttpClient для интеграции с Steam

## 📋 API Endpoints

### Games
- `GET /api/v1/games?month=2025-11` - список релизов
- `GET /api/v1/games/calendar?month=2025-11` - календарь релизов

### Analytics
- `GET /api/v1/analytics/top-genres` - топ-5 жанров
- `GET /api/v1/analytics/dynamics` - динамика изменений

## 🐳 Запуск через Docker

```bash
docker-compose up --build