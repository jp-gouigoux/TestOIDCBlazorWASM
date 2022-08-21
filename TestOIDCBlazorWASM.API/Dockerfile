#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestOIDCBlazorWASM.API/TestOIDCBlazorWASM.API.csproj", "TestOIDCBlazorWASM.API/"]
COPY ["TestOIDCBlazorWASM.Work/TestOIDCBlazorWASM.Work.csproj", "TestOIDCBlazorWASM.Work/"]
COPY ["TestOIDCBlazorWASM/Shared/TestOIDCBlazorWASM.Shared.csproj", "TestOIDCBlazorWASM/Shared/"]
RUN dotnet restore "TestOIDCBlazorWASM.API/TestOIDCBlazorWASM.API.csproj"
COPY . .
WORKDIR "/src/TestOIDCBlazorWASM.API"
RUN dotnet build "TestOIDCBlazorWASM.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestOIDCBlazorWASM.API.csproj" -c Release -o /app/publish -p:BlazorEnableCompression=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestOIDCBlazorWASM.API.dll"]