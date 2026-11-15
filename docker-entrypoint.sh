#!/bin/sh
# Punto de entrada del contenedor.
#
# Objetivos:
#   1. Garantizar que el directorio de datos exista y sea escribible.
#   2. Soltar privilegios: la aplicacion nunca corre como root.
#
# Por que existe este script:
#   Con un bind mount del host (docker run -v ./data:/data) el directorio queda
#   con el propietario del usuario del host y el usuario 'app' (UID 1654) de la
#   imagen oficial de .NET no puede escribir en el. Como el contenedor arranca
#   como root, aqui se corrige el propietario del directorio y despues se bajan
#   los privilegios, de modo que el proceso final sigue sin privilegios y el
#   bind mount del host sigue siendo utilizable.
#
# Variables de entorno:
#   AJN_DATA_DIR  Directorio de datos de la app (por defecto /data).

set -eu

DATA_DIR="${AJN_DATA_DIR:-/data}"

# El directorio de datos siempre tiene que existir.
mkdir -p "$DATA_DIR"

# Si ya corremos como un usuario sin privilegios no hay nada que hacer.
if [ "$(id -u)" -ne 0 ]; then
    exec "$@"
fi

# uid/gid reales del usuario 'app' en la imagen oficial de .NET.
APP_UID="$(id -u app 2>/dev/null || echo 1654)"
APP_GID="$(id -g app 2>/dev/null || echo 1654)"

# El directorio de datos (con sus archivos) queda en manos del usuario de la app.
if ! chown -R "${APP_UID}:${APP_GID}" "$DATA_DIR" 2>/dev/null; then
    echo "AVISO: no se pudo cambiar el propietario de ${DATA_DIR} a ${APP_UID}:${APP_GID}" >&2
fi

# Soltamos privilegios antes de ejecutar la aplicacion.
if command -v setpriv >/dev/null 2>&1; then
    exec setpriv --reuid=app --regid=app --init-groups "$@"
elif command -v su >/dev/null 2>&1; then
    exec su -s /bin/sh app -c 'exec "$@"' sh "$@"
else
    echo "AVISO: no se encontro setpriv ni su; la aplicacion se ejecuta como root" >&2
    exec "$@"
fi
