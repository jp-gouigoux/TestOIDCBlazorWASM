#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestOIDCBlazorWASM/Server/TestOIDCBlazorWASM.Server.csproj", "TestOIDCBlazorWASM/Server/"]
COPY ["TestOIDCBlazorWASM.Work/TestOIDCBlazorWASM.Work.csproj", "TestOIDCBlazorWASM.Work/"]
COPY ["TestOIDCBlazorWASM/Shared/TestOIDCBlazorWASM.Shared.csproj", "TestOIDCBlazorWASM/Shared/"]
COPY ["TestOIDCBlazorWASM/Client/TestOIDCBlazorWASM.Client.csproj", "TestOIDCBlazorWASM/Client/"]
RUN dotnet restore "TestOIDCBlazorWASM/Server/TestOIDCBlazorWASM.Server.csproj"
COPY . .
WORKDIR "/src/TestOIDCBlazorWASM/Server"
RUN dotnet build "TestOIDCBlazorWASM.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestOIDCBlazorWASM.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV OIDC__TargetUserRolesClaimName=user_roles
ENV OIDC__Audience=account
ENV OIDC__NameClaimType=preferred_username
ENV OIDC__ModelePourRoleClaim=resource_access.\$\{client_id\}.roles
ENTRYPOINT ["dotnet", "TestOIDCBlazorWASM.Server.dll"]