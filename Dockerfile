  FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
  WORKDIR /app
  EXPOSE 8080

  FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
  WORKDIR /src
  COPY ["API/API.csproj", "API/"]
  COPY ["BL/BL.csproj", "BL/"]
  COPY ["DAL/DAL.csproj", "DAL/"]
  COPY ["Domain/Domain.csproj", "Domain/"]
  RUN dotnet restore "API/API.csproj"
  COPY . .
  RUN dotnet build "API/API.csproj" -c Release -o /app/build

  FROM build AS publish
  RUN dotnet publish "API/API.csproj" -c Release -o /app/publish

  FROM base AS final
  RUN apt-get update && apt-get upgrade -y && apt-get install -y libfontconfig1 libfreetype6 && rm -rf /var/lib/apt/lists/*
  WORKDIR /app
  COPY --from=publish /app/publish .
  ENTRYPOINT ["dotnet", "SynapseEnqueteManagement.API.dll"]