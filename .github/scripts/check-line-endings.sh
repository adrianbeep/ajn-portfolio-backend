#!/bin/sh
# Verifica que el repo no reintroduzca finales de linea CRLF.
#
# Por que importa:
# el contenido de un .cs se compila dentro de la aplicacion (por ejemplo, los
# raw string literals que guardan el markdown sembrado). El compilador embebe
# los saltos de linea tal cual estan en el archivo, asi que un CRLF produce un
# modelo de EF Core distinto del que se genero en otra plataforma. Cuando el
# snapshot y el modelo no coinciden byte a byte, EF aborta el arranque con
# PendingModelChangesWarning y la aplicacion no levanta.
#
# Ver issue #5.

set -eu

fail=0

# 1. Ningun archivo versionado puede entrar al indice con CRLF.
offenders="$(git ls-files --eol | grep -E 'i/crlf' || true)"
if [ -n "$offenders" ]; then
	echo "::error::Archivos versionados con CRLF en el indice:"
	echo "$offenders"
	fail=1
fi

# 2. Todo .cs tiene que resolver a eol=lf. Si alguien saca la regla de
#    .gitattributes, el working tree de Windows vuelve a CRLF y el problema
#    reaparece sin que nada lo detecte.
not_lf="$(git ls-files '*.cs' | git check-attr --stdin eol | grep -v ': eol: lf$' || true)"
if [ -n "$not_lf" ]; then
	echo "::error::Archivos .cs sin el atributo eol=lf:"
	echo "$not_lf"
	fail=1
fi

if [ "$fail" -ne 0 ]; then
	exit 1
fi

echo "OK: finales de linea verificados (indice y working tree en LF)"
