#!/bin/bash
set -e

echo "Esperando a que ksqlDB server esté listo..."
until curl -s http://ksqldb-server:8088/info; do
  sleep 2
done

echo "Ejecutando script SQL..."
ksql http://ksqldb-server:8088 -f /ksql-scripts/ksql_setup.sql
echo "Script ejecutado correctamente."
