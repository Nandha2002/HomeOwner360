# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Homeowner360.Api/Homeowner360.Api.csproj Homeowner360.Api/
RUN dotnet restore Homeowner360.Api/Homeowner360.Api.csproj

COPY Homeowner360.Api/ Homeowner360.Api/

WORKDIR /src/Homeowner360.Api

RUN dotnet publish Homeowner360.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Homeowner360.Api.dll"]