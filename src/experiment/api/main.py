from fastapi import FastAPI, Response
import requests
import csv
import io

app = FastAPI()

KSQLDB_URL = "http://ksqldb-server:8088/query"

HEADERS = {
    "Content-Type": "application/vnd.ksql.v1+json; charset=utf-8",
    "Accept": "application/vnd.ksql.v1+json"
}

def query_ksql(sql: str):
    """Run a ksqlDB pull query and return structured data"""
    payload = {"ksql": sql, "streamsProperties": {}}
    resp = requests.post(KSQLDB_URL, json=payload, headers=HEADERS)
    resp.raise_for_status()
    result = resp.json()

    # Extract column names from metadata
    columns = []
    for item in result:
        if "header" in item and "schema" in item["header"]:
            schema = item["header"]["schema"]
            # schema example: 'STRUCT<PIZZAID INTEGER, ORDERID INTEGER, STARTTIMESTAMP BIGINT>'
            schema = schema.strip("STRUCT<>")
            
            # --- MODIFIED LOGIC HERE ---
            # Extract column names and strip any surrounding backticks (`)
            columns = [s.split()[0].strip('`') for s in schema.split(",")]
            # ---------------------------
            break

    # Extract rows
    rows = []
    for item in result:
        if "row" in item and "columns" in item["row"]:
            rows.append(item["row"]["columns"])

    return columns, rows

# -----------------------
# Dynamic JSON endpoint
# -----------------------
@app.get("/ksql/{table_name}/json")
def table_json(table_name: str, limit: int = 100):
    sql = f"SELECT * FROM {table_name} LIMIT {limit};"
    columns, rows = query_ksql(sql)
    return {"columns": columns, "rows": rows}

# -----------------------
# Dynamic CSV endpoint
# -----------------------
@app.get("/ksql/{table_name}/csv")
def table_csv(table_name: str, limit: int = 100):
    sql = f"SELECT * FROM {table_name} LIMIT {limit};"
    columns, rows = query_ksql(sql)

    output = io.StringIO()
    writer = csv.writer(output)

    if columns:
        writer.writerow(columns)
    for row in rows:
        writer.writerow(row)

    return Response(content=output.getvalue(), media_type="text/csv")
