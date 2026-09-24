FROM mcr.microsoft.com/dotnet/sdk:8.0.415-bookworm-slim AS build
WORKDIR /src
COPY api/src/Tbtb.Api/Tbtb.Api.csproj api/src/Tbtb.Api/
COPY api/src/Tbtb.Application/Tbtb.Application.csproj api/src/Tbtb.Application/
COPY api/src/Tbtb.Domain/Tbtb.Domain.csproj api/src/Tbtb.Domain/
COPY api/src/Tbtb.Infrastructure/Tbtb.Infrastructure.csproj api/src/Tbtb.Infrastructure/
RUN dotnet restore api/src/Tbtb.Api/Tbtb.Api.csproj
COPY . .
RUN dotnet publish api/src/Tbtb.Api/Tbtb.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0.19-bookworm-slim AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tbtb.Api.dll"]
