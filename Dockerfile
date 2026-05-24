# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["finance-tracker-backend/finance-tracker-backend.csproj", "finance-tracker-backend/"]
RUN dotnet restore "finance-tracker-backend/finance-tracker-backend.csproj"

COPY . .
RUN dotnet publish "finance-tracker-backend/finance-tracker-backend.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet finance-tracker-backend.dll --urls http://0.0.0.0:${PORT:-8080}"]
