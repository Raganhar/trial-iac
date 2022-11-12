FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DockerWebAPI.CDK/DockerWebAPI.CDK.csproj", "DockerWebAPI.CDK/"]
RUN dotnet restore "DockerWebAPI.CDK/DockerWebAPI.CDK.csproj"
COPY . .
WORKDIR "/src/DockerWebAPI.CDK"
RUN dotnet build "DockerWebAPI.CDK.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DockerWebAPI.CDK.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerWebAPI.CDK.dll"]
