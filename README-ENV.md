

# Paramètres pour l'IAM
KEYCLOAK_ADMIN=armoire
KEYCLOAK_ADMIN_PASSWORD=vBWtB2PloopC042cszXZ
KEYCLOAK_REALM=LivreENI
KEYCLOAK_CLIENT=appli-eni
IAM_DB_NAME=keycloak
IAM_DB_USER=postgres
IAM_DB_PASSWORD=KY9CO8sHshzM5YxfLtzL

# Paramètres pour le MOM
RABBIT_USER=rapido
RABBIT_PASSWORD=k5rXH6wmBhE2bukfXFsz
RABBIT_QUEUE_NAME=personnes

# Paramètres pour l'éditique
GED_USER=admin
GED_PASSWORD=admin
CMIS_BROWSER_PATH=lightweightcmis/browser
CMIS_ACCESS_PATH="default/root/?cmisselector=content&objectId={idDoc}"

Attention, ces deux variables sont des morceaux de chemins qui doivent être exprimés en relatif, car les HOST sont gérés par le Docker Compose et les hôtes d'exposition par Caddy. Le choix de composition est que les séparateurs seront rajoutés par le mécanisme autour des valeurs de ces paramètres, donc il est inutile (et peut parfois poser problème) de rajouter des `/` au début ou à la fin de ces paramètres.

A noter que les guillemets ne sont pas obligatoires, mais peuvent simplifier la lecture dans votre éditeur et rendent explicite le fait que la syntaxe {idDoc} fait partie intégrante de la chaîne à envoyer et sera interprétée par l'applicatif lui-même, et pas l'architecture Docker.

# Paramètres pour l'application
MONGO_DB_NAME=personnes
MONGO_DB_COLLECTION=personnes
THUMBPRINT_CERTIFICAT=FDBE54F78826D8EB51F3617C5E3C7A7DCFAF722C