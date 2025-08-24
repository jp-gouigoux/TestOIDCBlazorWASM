# Ajustement de la configuration de l'application

Comme précisé dans le fichier `README.md`, l'application est censée fonctionner telle quelle une fois le code source récupéré, mais il est possible de changer facilement les paramètres en éditant les fichiers prévus à cet effet. Le présent fichier décrit ce mécanisme.

## Seul paramètre obligatoire à ajuster

Le fichier `.env` contient la plupart des paramètres configurables, et en particulier `DEPLOY_HOST` qui permet de spécifier la première partie de l'URL, qui sera commune à tous les services exposés. Dans le fichier fourni, la valeur est `https://dockereni.francecentral.cloudapp.azure.com` car l'application a été exposée sur une machine virtuelle lancée sur Azure. Le protocole est HTTPS car l'application utilise un reverse-proxy qui s'occupe de l'exposition en SSL. Il ne faut pas utiliser un `/` final dans la valeur de ce paramètre.

Cette valeur doit obligatoirement être changée, car vous serez nécessairement sur un environnement différent de celui servant d'exemple (sauf bien sûr le cas particulier où vous utilisez `https://localhost`, correspondant à un fonctionnement sur votre machine locale, ce qui peut être le plus simple même si cela est un peu plus lointain d'une utilisation en production réaliste).

Pour être vraiment précis, un second paramètre doit être modifié pour que l'application fonctionne vraiment, à savoir l'empreinte du fichier certificat généré comme expliqué dans le fichier [README.md](./README.md). En effet, le certificat exemple fourni dans le présent projet n'a qu'une durée de vie limitée. De plus, l'autorité de certification n'est pas fournie.

## Autres paramètres de l'application

Tous les autres paramètres du fichier `.env` peuvent être laissés tel quel sans que cela n'affecte le fonctionnement de l'exemple. Même s'il ne s'agit que d'un exemple, il est de bonne pratique de modifier les mots de passe pour incorporer les vôtres et faire bien sûr en sorte qu'il soit dédiés chacun à un seul usage.

### Paramètres pour l'IAM

La première section correspond aux settings nécessaires pour l'authentification et identification, en l'occurrence réalisée par un serveur Apache Keycloak :
1. KEYCLOAK_REALM : le nom du royaume Keycloak à utiliser.
2. KEYCLOAK_CLIENT : identifiant du client d'authentification défini dans Keycloak pour l'application.
3. IAM_DB_NAME : nom de la base de données pour la persistance de l'IAM.
4. IAM_DB_USER : login du compte à utiliser pour la connexion à cette base de données PostgreSQL.
5. IAM_DB_PASSWORD : mot de passe associé à ce compte.

Ce qui ne relève pas de l'interaction de l'application avec l'IAM, mais bien de l'IAM elle-même (en l'occurrence, les accès administrateurs) a été mis dans un fichier à part, nommé `.env-iam`, et qui est obligatoire pour le lancement dans Docker Compose. Ce fichier est présenté en détails un peu plus bas.

L'intérêt de définir de manière centralisée les valeurs pour l'interaction entre l'IAM et sa base de données de persistence est d'éviter ainsi les problèmes dûs à une erreur de saisir ; grâce au mécanisme d'interpolation de Docker Compose, on peut en effet les définir une seule fois pour les deux services, ce qui garantit la synchronisation.

### Paramètres pour le MOM

La seconde section de paramétrage du fichier `.env` correspond à ce qui est requis pour le fonctionnement du Middleware Orienté Message, à savoir RabbitMQ dans cette application :
1. RABBIT_USER : le login de l'identifiant RabbitMQ à utiliser.
2. RABBIT_PASSWORD : son mot de passe associé.
3. RABBIT_QUEUE_NAME : le nom de la file de messages à utiliser pour véhiculer les informations sur les personnes crées dans l'application métier entre le serveur et le récepteur pour traitement.

Pour cette section, on considère que le user pour le MOM est dédié à l'application, ainsi que le nom de la file de message. Il n'y a donc pas lieu de mettre en place un fichier séparé pour faciliter la gestion.

### Paramètres pour l'éditique

La troisième section correspond aux paramètres applicatifs pour l'éditique et la Gestion Electronique de Documents. Une GED compatible CMIS 1.1 sera utilisée et les appels sont donc standardisés, mais certaines expositions ne sont pas définies par le standard et devront donc être indiquées :
1. CMIS_BROWSER_PATH : la partie de l'URL qui doit être rajoutée après l'hôte, pour accéder au binding de type browser (dans notre exemple, la GED utilisée est une application open source nommée LightweightCMISServer, et son chemin d'exposition du endpoint est `lightweightcmis/browser`).
2. CMIS_ACCESS_PATH : la portion d'URL à rajouter au paramètre précédent pour accéder à un document directement en GET, sans passer par une `cmisaction`, et qui doit préciser où sera inséré l'identifiant du document à l'aide de l'expression `{idDoc}` (dans notre cas, ce sera `default/root/?cmisselector=content&objectId={idDoc}`). A noter que vous pouvez utiliser des guillemets pour entourer cette valeur pour rendre explicite que les accolades utilisées dans cette syntaxe font partie de la valeur de la chaîne (l'expression sera interprétée par l'applicatif lui-même, pas par l'architecture de gestion des paramètres Docker).

Attention, ces deux variables sont des morceaux de chemins qui doivent être exprimés en relatif, car les HOST sont gérés par le Docker Compose et les hôtes d'exposition par Caddy. Le choix de composition est que les séparateurs seront rajoutés par le mécanisme autour des valeurs de ces paramètres, donc il est inutile (et peut parfois poser problème) de rajouter des `/` au début ou à la fin de ces paramètres.

Comme pour la première section, il a été décidé de recourir à un fichier additionnel pour certains paramètres, à savoir les crédentiels pour accéder à la GED. Ceci ne manifeste pas qu'ils ne relèvent pas de la liaison avec l'application (elle les utilise pour se connecter à l'API CMIS), mais qu'ils sont davantage liés à l'image de GED utilisée. Il est donc plus logique que le fichier soit externalisé ; de plus, cela permettrait de synchroniser à terme les paramètres avec ceux utilisés pour la compilation de l'image Docker de la GED ciblée. Dans notre cas, ce fichier `.env-ged` contient le couple `admin/admin` qui est utilisé par défaut. Mais cela aurait du sens que l'éditeur de l'image fasse évoluer vers plus de sécurité et oblige à spécifier a minima un mot de passe à l'initialisation. Nous pourrons alors faire évoluer le fichier `.env-ged` en fonction. A noter que, comme des valeurs par défaut existent, le fichier est marqué en `required: false` dans `compose.yaml`, et aucune erreur ne sera donc généré s'il n'existe pas dans le même chemin que ce dernier fichier lors du lancement de l'application par Docker Compose.

### Paramètres pour l'application métier

La dernière section de `.env` contient les paramètres pour l'application métier elle-même (et non pas les dépendances, que nous venons de parcourir) :
1. MONGO_DB_NAME : le nom de la base de données pour le stockage des informations de personnes manipulées par l'application.
MONGO_DB_COLLECTION : le nom de la collection MongoDB utilisée dans cette base de données.
THUMBPRINT_CERTIFICAT : l'empreinte du certificat X509 utilisée pour la communication sécurisée à l'API (voir le fichier [README.md](./README.md) pour plus de détails sur la mise en oeuvre de ces certificats).

## Fichiers de configuration additionnels

Comme précisé plus haut, deux fichiers comportent des paramètres additionnels, déportés du fichier principal de paramétrage `.env` car ils concernent plus l'application satellite associée que l'ensemble applicatif complet.

### Paramétrage interne de l'IAM

Le premier est le fichier `.env-iam`. Il contient les données nécessaires pour le paramétrage de l'IAM Keycloak :
1. KEYCLOAK_ADMIN : le code du compte administrateur de Keycloak.
2. KEYCLOAK_ADMIN_PASSWORD : son mot de passe.

Ce fichier est obligatoire et son absence générera une erreur au lancement de Docker Compose. En effet, si on ne paramètre pas les accès administrateurs au démarrage du conteneur, on ne pourra pas avoir accès à la configuration des royaumes Keycloak. Et si on ne dipose d'aucun moyen de s'authentifier à l'application, qui nécessite un accès identifié à la norme OpenID Connect, alors cette dernière ne pourra pas être accédée autrement que sur les pages publiques.

### Paramétrage interne de la GED

Le fichier `.env-ged` contient les informations de paramétrage de la GED :
1. GED__ServiceAccountName : le code du compte à utiliser pour accéder au protocole CMIS (ainsi qu'à l'exposition web directe, même si on pourrait à terme séparer les deux comptes pour améliorer la sécurité).
2. GED__ServiceAccountPassword : le mot de passe associé à ce compte.

Ce fichier n'est pas obligatoire, contrairement à `.env-iam`. S'il est absent, les valeurs `admin` seront utilisées, ce qui correspond au comportement par défaut de LightweightCMISServer à l'heure actuelle.

A noter que la syntaxe de ces noms de variables indiquent qu'ils seront utilisés en fait dans l'application .NET, le mécanisme de configuration utilisant les caractères `__` pour délimiter la section de configuration et le nom de l'attribut.
