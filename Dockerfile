FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Ftareqi.API/Ftareqi.API.csproj", "Ftareqi.API/"]
COPY ["Ftareqi.Application/Ftareqi.Application.csproj", "Ftareqi.Application/"]
COPY ["Ftareqi.Domain/Ftareqi.Domain.csproj", "Ftareqi.Domain/"]
COPY ["Ftareqi.Infrastructure/Ftareqi.Infrastructure.csproj", "Ftareqi.Infrastructure/"]
COPY ["Ftareqi.Persistence/Ftareqi.Persistence.csproj", "Ftareqi.Persistence/"]
RUN dotnet restore "Ftareqi.API/Ftareqi.API.csproj"
COPY . .
WORKDIR "/src/Ftareqi.API"
RUN dotnet build "Ftareqi.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ftareqi.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ftareqi.API.dll"]