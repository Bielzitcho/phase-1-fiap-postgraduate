FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore as a distinct cache layer
COPY OficinaTech.sln .
COPY src/OficinaTech.Domain/OficinaTech.Domain.csproj src/OficinaTech.Domain/
COPY src/OficinaTech.Application/OficinaTech.Application.csproj src/OficinaTech.Application/
COPY src/OficinaTech.Infrastructure/OficinaTech.Infrastructure.csproj src/OficinaTech.Infrastructure/
COPY src/OficinaTech.Presentation/OficinaTech.Presentation.csproj src/OficinaTech.Presentation/
COPY tests/OficinaTech.Tests/OficinaTech.Tests.csproj tests/OficinaTech.Tests/
RUN dotnet restore src/OficinaTech.Presentation/OficinaTech.Presentation.csproj

# Build and publish
COPY . .
RUN dotnet publish src/OficinaTech.Presentation/OficinaTech.Presentation.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "OficinaTech.Presentation.dll"]
