FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DietPlanner.sln ./
COPY src/DietPlanner.Api/DietPlanner.Api.csproj src/DietPlanner.Api/
COPY src/DietPlanner.Application/DietPlanner.Application.csproj src/DietPlanner.Application/
COPY src/DietPlanner.Domain/DietPlanner.Domain.csproj src/DietPlanner.Domain/
COPY src/DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj src/DietPlanner.Infrastructure/

RUN dotnet restore src/DietPlanner.Api/DietPlanner.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/DietPlanner.Api/DietPlanner.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet DietPlanner.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
