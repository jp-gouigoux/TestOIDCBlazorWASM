FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RecepteurMessages/RecepteurMessages.csproj", "RecepteurMessages/"]
COPY ["TestOIDCBlazorWASM/Shared/TestOIDCBlazorWASM.Shared.csproj", "TestOIDCBlazorWASM/Shared/"]
RUN dotnet restore "RecepteurMessages/RecepteurMessages.csproj"
COPY . .
WORKDIR "/src/RecepteurMessages"
RUN dotnet build "RecepteurMessages.csproj" -c Release -o /app/build
RUN dotnet publish "RecepteurMessages.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RecepteurMessages.dll"]