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

    columns = []
    for item in result:
        if "header" in item and "schema" in item["header"]:
            schema = item["header"]["schema"]
            schema = schema.strip("STRUCT<>")
            columns = [s.split()[0].strip('`') for s in schema.split(",")]
            break

    rows = []
    for item in result:
        if "row" in item and "columns" in item["row"]:
            rows.append(item["row"]["columns"])

    return columns, rows

def ksql_csv_response(sql: str):
    columns, rows = query_ksql(sql)
    output = io.StringIO()
    writer = csv.writer(output)

    if columns:
        writer.writerow(columns)
    for row in rows:
        writer.writerow(row)

    return Response(content=output.getvalue(), media_type="text/csv")


# -----------------------
# Pizza & Order endpoints
# -----------------------
@app.get("/ksql/order_latency")
def order_latency_csv():
    sql = f"SELECT * FROM order_latency;"
    return ksql_csv_response(sql)

@app.get("/ksql/pizza_latency")
def pizza_latency_csv():
    sql = f"SELECT * FROM pizza_latency;"
    return ksql_csv_response(sql)
@app.get("/ksql/order_dispatch_latency")
def order_dispatched_latency_csv():
    sql = f"SELECT * FROM order_dispatch_latency;"
    return ksql_csv_response(sql)



# -----------------------
# Restock endpoints
# -----------------------

@app.get("/ksql/dough_machine_restock_latency")
def dough_machine_restock_csv():
    sql = f"SELECT * FROM dough_machine_restock_latency;"
    return ksql_csv_response(sql)


@app.get("/ksql/sauce_machine_restock_latency")
def sauce_machine_restock_csv():
    sql = f"SELECT * FROM sauce_machine_restock_latency;"
    return ksql_csv_response(sql)


@app.get("/ksql/cheese_machine_restock_latency")
def cheese_machine_restock_csv():
    sql = f"SELECT * FROM cheese_machine_restock_latency;"
    return ksql_csv_response(sql)


@app.get("/ksql/meat_machine_restock_latency")
def meat_machine_restock_csv():
    sql = f"SELECT * FROM meat_machine_restock_latency;"
    return ksql_csv_response(sql)


@app.get("/ksql/vegetables_machine_restock_latency")
def vegetables_machine_restock_csv():
    sql = f"SELECT * FROM vegetables_machine_restock_latency;"
    return ksql_csv_response(sql)


@app.get("/ksql/packaging_machine_restock_latency")
def packaging_machine_restock_csv():
    sql = f"SELECT * FROM packaging_machine_restock_latency;"
    return ksql_csv_response(sql)
