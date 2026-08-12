FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
RUN dotnet restore "src/NotificationService/NotificationService.csproj"

COPY . .
WORKDIR /src/src/NotificationService
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
