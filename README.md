# RaceManager

## Запуск веб-приложения

1. Установить зависимости один раз:

```bash
npm install
npm --prefix client install
```

2. Запустить backend и frontend одной командой из папки `RaceManager`:

```bash
npm run dev
```

После запуска:

- Frontend: http://127.0.0.1:5173/
- Backend API: http://127.0.0.1:5088/

Vite проксирует запросы `/api` на C# backend.
