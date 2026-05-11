FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

COPY ["MultiToolBot/ProjectHost.csproj", "MultiToolBot/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["PermGorTrans/PermGorTrans.ApiClient.csproj", "PermGorTrans/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["TelegramStopBot/TelegramStopBot.csproj", "TelegramStopBot/"]
RUN dotnet restore "./MultiToolBot/ProjectHost.csproj"
COPY . .
WORKDIR "/src/MultiToolBot"
RUN dotnet build "./ProjectHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProjectHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

WORKDIR "/src"
ENV ConnectionStrings__DefaultConnection="Host=localhost;Database=mock"
ENV BotSettings__Token="0000000000:AA-MOCK-TOKEN-FOR-BUILD"


RUN dotnet ef migrations bundle \
    --project Infrastructure/Infrastructure.csproj \
    --startup-project MultiToolBot/ProjectHost.csproj \
    --self-contained \
    -r linux-musl-x64 \
    -o /app/publish/efbundle \
    --configuration $BUILD_CONFIGURATION

FROM base AS final
WORKDIR /app

USER root

RUN apk add --no-cache krb5-libs

COPY --from=publish /app/publish .
RUN chmod +x /app/efbundle
USER $APP_UID

ENTRYPOINT ["dotnet", "ProjectHost.dll"]