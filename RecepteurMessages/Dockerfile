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
ARG URL_XKCD=https://xkcd.com/{indexImage}/info.0.json
ARG MAXINDEX_XKCD=614
WORKDIR /app
COPY --from=build /app/publish .
ENV XKCD__TemplateURLAPI=${URL_XKCD}
ENV XKCD__MaximumIndexImage=${MAXINDEX_XKCD}
ENV DelaiSupplementaireTraitementEnMillisecondes=0
ENV GED__NomRepertoireStockageFichesPersonnes=repertoire
ENV Securite__CheminFichierCertificatClient=clienttestoidc.pfx
ENV URLBaseServiceAPI=https://api
ENTRYPOINT ["dotnet", "RecepteurMessages.dll"]
# CMD ["RabbitMQ__HoteServeur=localhost", "RabbitMQ__NomQueueMessagesCreationPersonnes=personnes", "GED__URLAtomPub=http://localhost:9000/nuxeo/atom/cmis", "GED__ServiceAccountName=Administrator", "GED__ModeleURLExpositionDirecteDocuments=http://localhost:9000/nuxeo/atom/cmis/default/content/{nomFichier}?id={idDoc}"]
