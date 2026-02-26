# --- Этап 1: Сборка (build stage) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проектов для восстановления зависимостей (кэшируется Docker-ом)
COPY SemMiniHttpServer/SemMiniHttpServer.csproj SemMiniHttpServer/
COPY MyORMLibrary/MyORMLibrary.csproj MyORMLibrary/
COPY MiniTemplateEngine/MiniTemplateEngine.csproj MiniTemplateEngine/

# Восстанавливаем NuGet-зависимости
RUN dotnet restore SemMiniHttpServer/SemMiniHttpServer.csproj

# Копируем весь исходный код всех проектов
COPY SemMiniHttpServer/ SemMiniHttpServer/
COPY MyORMLibrary/ MyORMLibrary/
COPY MiniTemplateEngine/ MiniTemplateEngine/

# Собираем проект в Release-режиме
RUN dotnet publish SemMiniHttpServer/SemMiniHttpServer.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Этап 2: Запуск (runtime stage) ---
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Копируем собранное приложение из build-этапа
COPY --from=build /app/publish .

# Открываем порт приложения
EXPOSE 8080

# Запуск приложения
ENTRYPOINT ["dotnet", "SemMiniHttpServer.dll"]