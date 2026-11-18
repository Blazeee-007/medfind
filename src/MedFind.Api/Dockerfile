FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["src/MedFind.Domain/MedFind.Domain.csproj", "src/MedFind.Domain/"]
COPY ["src/MedFind.Application/MedFind.Application.csproj", "src/MedFind.Application/"]
COPY ["src/MedFind.Infrastructure/MedFind.Infrastructure.csproj", "src/MedFind.Infrastructure/"]
COPY ["src/MedFind.Api/MedFind.Api.csproj", "src/MedFind.Api/"]
RUN dotnet restore "src/MedFind.Api/MedFind.Api.csproj"

COPY . .
RUN dotnet build "src/MedFind.Api/MedFind.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/MedFind.Api/MedFind.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedFind.Api.dll"]
