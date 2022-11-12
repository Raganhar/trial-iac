FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DockerWebAPI.AWSCDK/DockerWebAPI.AWSCDK.csproj", "DockerWebAPI.AWSCDK/"]
RUN dotnet restore "DockerWebAPI.AWSCDK/DockerWebAPI.AWSCDK.csproj"
COPY . .
WORKDIR "/src/DockerWebAPI.AWSCDK"
RUN dotnet build "DockerWebAPI.AWSCDK.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DockerWebAPI.AWSCDK.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerWebAPI.AWSCDK.dll"]
