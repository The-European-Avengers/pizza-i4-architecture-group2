from fastapi import FastAPI, Response
import requests
import pandas as pd
from fastapi.responses import StreamingResponse
from io import StringIO

app = FastAPI(title="Pizza Production Metrics API")

KSQLDB_SERVER = "http://ksqldb-server:8088"

def query_ksql(sql: str):
    """Query ksqlDB and return JSON results."""
    payload = {"ksql": sql, "streamsProperties": {}}
    resp = requests.post(f"{KSQLDB_SERVER}/query", json=payload, stream=True)
    resp.raise_for_status()
    data = []
    for line in resp.iter_lines():
        if line:
            row = line.decode("utf-8")
            if row.startswith("{"):
                row_json = pd.json.loads(row)
                if "row" in row_json:
                    data.append(row_json["row"]["columns"])
    return data

# ---------- Endpoints ----------

@app.get("/order_latency/csv")
def order_latency_csv():
    sql = "SELECT ORDERID, ORDERSIZE, STARTTIMESTAMP, ENDTIMESTAMP, LATENCYMS FROM order_latency EMIT CHANGES;"
    data = query_ksql(sql)
    df = pd.DataFrame(data, columns=["ORDERID", "ORDERSIZE", "STARTTIMESTAMP", "ENDTIMESTAMP", "LATENCYMS"])
    
    csv_buffer = StringIO()
    df.to_csv(csv_buffer, index=False)
    csv_buffer.seek(0)
    
    return StreamingResponse(csv_buffer, media_type="text/csv", headers={"Content-Disposition": "attachment; filename=order_latency.csv"})

@app.get("/pizza_latency/table")
def pizza_latency_table():
    sql = "SELECT PIZZAID, ORDERID, STARTTIMESTAMP, ENDTIMESTAMP, LATENCYMS FROM pizza_latency EMIT CHANGES;"
    data = query_ksql(sql)
    df = pd.DataFrame(data, columns=["PIZZAID", "ORDERID", "STARTTIMESTAMP", "ENDTIMESTAMP", "LATENCYMS"])
    return df.to_dict(orient="records")
