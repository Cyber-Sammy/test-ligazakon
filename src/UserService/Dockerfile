FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/UserService/UserService.csproj", "src/UserService/"]
RUN dotnet restore "src/UserService/UserService.csproj"

COPY . .
WORKDIR /src/src/UserService
RUN dotnet publish "UserService.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UserService.dll"]
