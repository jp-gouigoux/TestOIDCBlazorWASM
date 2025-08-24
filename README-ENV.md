

# Paramètres pour l'IAM
KEYCLOAK_REALM=LivreENI
KEYCLOAK_CLIENT=appli-eni
IAM_DB_NAME=keycloak
IAM_DB_USER=postgres
IAM_DB_PASSWORD=KY9CO8sHshzM5YxfLtzL

Ce qui ne relève pas de l'interaction de l'application avec l'IAM, mais bien de l'IAM elle-même (en l'occurrence, les accès administrateurs) a été mis dans un fichier à part. Comme on n'indique rien, le fichier est requis par Docker Compose. Ce comportement est logique car, si on ne paramètre pas les accès administrateurs au démarrage du conteneur, on ne pourra pas avoir accès à la configuration des royaumes Keycloak, et donc aucun moyen de s'authentifier à l'application, qui rend obligatoire l'accès identifié à la norme OpenID Connect. L'intérêt de définir les valeurs pour l'interaction avec une base de données de persistence des paramétrages de l'IAM est aussi que, grâce au mécanisme d'interpolation de Docker Compose, on peut les définir une seule fois pour les deux services, ce qui évite les erreurs lors d'une recopie et les problèmes associés.

# Paramètres pour le MOM
RABBIT_USER=rapido
RABBIT_PASSWORD=k5rXH6wmBhE2bukfXFsz
RABBIT_QUEUE_NAME=personnes

Pour cette section, on considère que le user pour le MOM est dédié à l'application, ainsi que le nom de la file de message. Il n'y a donc pas lieu de mettre en place un fichier séparé pour faciliter la gestion.

# Paramètres pour l'éditique
CMIS_BROWSER_PATH=lightweightcmis/browser
CMIS_ACCESS_PATH="default/root/?cmisselector=content&objectId={idDoc}"

Attention, ces deux variables sont des morceaux de chemins qui doivent être exprimés en relatif, car les HOST sont gérés par le Docker Compose et les hôtes d'exposition par Caddy. Le choix de composition est que les séparateurs seront rajoutés par le mécanisme autour des valeurs de ces paramètres, donc il est inutile (et peut parfois poser problème) de rajouter des `/` au début ou à la fin de ces paramètres.

A noter que les guillemets ne sont pas obligatoires, mais peuvent simplifier la lecture dans votre éditeur et rendent explicite le fait que la syntaxe {idDoc} fait partie intégrante de la chaîne à envoyer et sera interprétée par l'applicatif lui-même, et pas l'architecture Docker.

Cette fois, ce sont les paramètres de définition des crédentiels pour accéder à la GED qui seront placés dans un fichier à part. Non pas parce qu'ils ne relèvent pas de la liaison avec l'application (elle les utilise pour se connecter à l'API CMIS), mais parce qu'ils sont plus liés à l'image de GED utilisée. Il est donc plus logique que le fichier soit externalisé ; de plus, cela permettrait de synchroniser à terme les paramètres avec ceux utilisés pour la compilation de l'image Docker de la GED ciblée. Dans notre cas, le couple `admin/admin` est utilisé par défaut, mais cela aurait du sens que l'éditeur de l'image fasse évoluer vers plus de sécurité. Nous pourrons alors faire évoluer le fichier `.env-ged` en fonction. Comme ce comportement est celui par défaut, le fichier est par ailleurs en `required: false` dans `compose.yaml`.

# Paramètres pour l'application
MONGO_DB_NAME=personnes
MONGO_DB_COLLECTION=personnes
THUMBPRINT_CERTIFICAT=FDBE54F78826D8EB51F3617C5E3C7A7DCFAF722C