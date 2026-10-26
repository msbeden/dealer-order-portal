# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy props and csproj files for optimal layer caching
COPY Directory.Build.props ./
COPY backend/src/DealerPortal.Domain/DealerPortal.Domain.csproj backend/src/DealerPortal.Domain/
COPY backend/src/DealerPortal.Application/DealerPortal.Application.csproj backend/src/DealerPortal.Application/
COPY backend/src/DealerPortal.Infrastructure/DealerPortal.Infrastructure.csproj backend/src/DealerPortal.Infrastructure/
COPY backend/src/DealerPortal.Api/DealerPortal.Api.csproj backend/src/DealerPortal.Api/

RUN dotnet restore backend/src/DealerPortal.Api/DealerPortal.Api.csproj

# Copy source code and build
COPY backend/src/ backend/src/
RUN dotnet publish backend/src/DealerPortal.Api/DealerPortal.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DealerPortal.Api.dll"]
