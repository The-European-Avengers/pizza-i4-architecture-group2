#!/bin/bash
set -e

echo "Waiting for ksqlDB server to be ready..."
until curl -s http://ksqldb-server:8088/info; do
  sleep 2
done

echo "ksqlDB server is ready. Executing KSQL script..."
ksql http://ksqldb-server:8088 -f /ksql-scripts/ksql_setup.sql
echo "KSQL script executed successfully."
