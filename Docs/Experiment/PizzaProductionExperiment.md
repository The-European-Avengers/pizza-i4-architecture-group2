# Production Line Experiment: Real-Time Pizza Factory

This experiment simulates a pizza production line using **Docker** and **Kafka** (specifically **Redpanda**) to model a distributed, event-driven system. Machines implemented in different programming languages communicate exclusively by producing and consuming structured JSON messages from shared Kafka topics.


## Quick Start

### 1\. Build and Run All Services 

All services (Kafka, KSQLDB, and the polyglot machines) are containerized. Run the following command from the root experiment folder (where your `docker-compose.yml` is). The **`--build`** flag ensures all services are compiled fresh.

```bash
docker-compose up --build
```

**What to Expect:**

  * You will first see the **`kafka-init`** service start. It connects to Kafka, creates all the necessary topics (e.g., `dough-machine`, `dough-machine-done`), and then exits successfully.
  * Once initialization is finished, Docker Compose automatically starts all other services (`order-processor`, `dough-machine`, etc.).
  * Wait for all services to log that they are **"subscribed"** and **"ready."**


### 2\. Monitor Kafka

To monitor the flow of messages and status of topics in real-time, open **Redpanda Console** in your browser:

```
http://localhost:8090/
```

### 3\. Run an Order 

The **OrderProcessing** service exposes an API to inject a new order into the system. Run any of the following commands in a **new terminal** to simulate a customer order:

| Command | Pizzas | Description |
| :--- | :--- | :--- |
| `curl -X POST http://localhost:8082/start-order/10` | **10** | A small test order. |
| `curl -X POST http://localhost:8082/start-order/50` | **50** | A standard-sized order. |
| `curl -X POST http://localhost:8082/start-order/100` | **100** | A large-scale test order. |

You can watch the logs in your `docker-compose` terminal to see the entire production line process the order one pizza at a time.


### 4\. Analyze Data

The KSQLDB tables calculate real-time latency. You can access this data via a simple web API for analysis.

| Data | Endpoint (JSON) | Endpoint (CSV Download) |
| :--- | :--- | :--- |
| **Order Latency** | `http://localhost:8000/ksql/order_latency/json` | `http://localhost:8000/ksql/order_latency/download` |
| **Pizza Latency** | `http://localhost:8000/ksql/pizza_latency/json` | `http://localhost:8000/ksql/pizza_latency/download` |


### 5\. Run a Clean Experiment 

Kafka saves all messages to a persistent data volume. If you simply restart the containers, the `order-processor` might re-read old "done" messages from the previous run, corrupting the new experiment data.

To run a **clean, new experiment**, you must stop and delete the containers **AND** the Kafka data volume:

```bash
docker-compose down -v
```

After this command finishes, you can safely go back to Step 1 (`docker-compose up --build`) to start a fresh run.


## Folder Structure and Technology Stack

Each service lives in its own folder and includes a dedicated `Dockerfile`. The production line is intentionally implemented using multiple programming languages to demonstrate polyglot microservices integration via Kafka.

### C\# 

| Service | Role |
| :--- | :--- |
| **KafkaInitializer** | Bootstraps the system by creating all required Kafka topics. |
| **OrderProcessing** | Manages incoming orders and dispatches pizzas to the first machine (`dough-machine`). |
| **DoughMachine** | Simulates the preparation of raw dough. |
| **DoughShaper** | Simulates shaping the dough into pizza bases. |
| **CheeseGrater** | Simulates grating and adding cheese to the pizza. |

### Python 

| Service | Role |
| :--- | :--- |
| **SauceMachine** | Chooses and applies the correct sauce to the pizza. |
| **Oven** | Handles the baking process. |
| **Freezer** | Handles the freezing step for pizzas that require freezing. |

### Go 

| Service | Role |
| :--- | :--- |
| **MeatSlicer** | Selects and applies meat toppings. |
| **VegetablesSlicer** | Selects and applies vegetable toppings. |
| **Packaging-robot** | Packages finished pizzas into boxes, signaling **`pizza-done`**. |


## Kafka Topics Architecture

The system uses a set of topics for the main workflow and another set for order tracking and observability.

### Production Line Topics

Each machine operates as a Consumer-Processor-Producer: it **consumes** from its assigned topic and **produces** to the next machine's topic. Additionally, every machine emits a small message to a dedicated `-done` topic to signal it is ready for the next pizza payload.

| Topic Pair | Consuming Machine | Producing Machine (Next Step) |
| :--- | :--- | :--- |
| `dough-machine` / `dough-machine-done` | **DoughMachine** | `dough-shaper` |
| `dough-shaper` / `dough-shaper-done` | **DoughShaper** | `sauce-machine` |
| `sauce-machine` / `sauce-machine-done` | **SauceMachine** | `cheese-machine` |
| `cheese-machine` / `cheese-machine-done` | **CheeseGrater** | `meat-machine` |
| `meat-machine` / `meat-machine-done` | **MeatSlicer** | `vegetables-machine` |
| `vegetables-machine` / `vegetables-machine-done` | **VegetablesSlicer** | `oven-machine` |
| `oven-machine` / `oven-machine-done` | **Oven** | `freezer-machine` or `packaging-machine` |
| `freezer-machine` / `freezer-machine-done` | **Freezer** | `packaging-machine` |
| `packaging-machine` / `packaging-machine-done` | **Packaging-robot** | **`pizza-done`** |

### Order Tracking and Management Topics

| Topic | Producer(s) | Purpose |
| :--- | :--- | :--- |
| **`pizza-done`** | Packaging-robot | Records every completed pizza for latency calculation. |
| **`order-processing`** | OrderProcessing | Emitted when an order **starts** processing. |
| **`order-done`** | OrderProcessing | Emitted when *all* pizzas for an order are **completed**. |
| **`order-stack`** | (Future Enhancement) | Queue of orders waiting to be processed (e.g., for priority queuing). |


### Restock Topics

These dedicated topics simulate machine **restocking** and allow for granular measurement of **Restock Latency** for each individual production step.

| Topic Pair | Request Producer | Acknowledgment Consumer | Purpose |
| :--- | :--- | :--- | :--- |
| `dough-machine-restock` / `dough-machine-restock-done` | **DoughMachine** | **DoughMachine** | Track dough ingredient replenishment latency. |
| `sauce-machine-restock` / `sauce-machine-restock-done` | **SauceMachine** | **SauceMachine** | Track sauce replenishment latency. |
| `cheese-machine-restock` / `cheese-machine-restock-done` | **CheeseGrater** | **CheeseGrater** | Track cheese ingredient replenishment latency. |
| `meat-machine-restock` / `meat-machine-restock-done` | **MeatSlicer** | **MeatSlicer** | Track meat topping replenishment latency. |
| `vegetables-machine-restock` / `vegetables-machine-restock-done`| **VegetablesSlicer** | **VegetablesSlicer** | Track vegetable topping replenishment latency. |
| `packaging-machine-restock` / `packaging-machine-restock-done`| **Packaging-robot** | **Packaging-robot** | Track packaging material replenishment latency. |


## Kafka Messages Schema

This section describes the four main message types exchanged between services.

### 1\. Pizza Order Message (The main payload)

This message describes the **current state of a single pizza** and is the primary payload that travels sequentially through all machines.

| Field | Type | Description |
| :--- | :--- | :--- |
| `pizzaId` | `int` | Unique ID for the pizza. |
| `orderId` | `string` | ID of the order this pizza belongs to. |
| `orderSize` | `int` | Total number of pizzas in the order. |
| `startTimestamp` | `long` | When the overall order processing began. |
| `endTimestamp` | `long` | When this specific pizza processing finished (nullable until complete). |
| `msgDesc` | `string` | Description of the current processing step. |
| `sauce` | `string` | Sauce type applied. |
| `baked` | `boolean` | Whether the pizza has been baked. |
| `cheese`, `meat`, `veggies` | `array<string>` | Toppings applied so far. |

**Example:**

```json
{
  "pizzaId": 42,
  "orderId": "0d956eaa-5cc8-4320-b62c-3ca8249085af",
  "orderSize": 3,
  "startTimestamp": 1731571200000,
  "endTimestamp": null,
  "msgDesc": "Sauce added to pizza",
  "sauce": "tomato",
  "baked": false,
  "cheese": ["mozzarella"],
  "meat": ["pepperoni", "sausage"],
  "veggies": ["mushroom", "onion"]
}
```

### 2\. Step Done Message (Machine Acknowledgement)

Sent by a machine to its dedicated `-done` topic after successfully processing a `Pizza Order Message`. This signals to the overall flow controller (OrderProcessing logic) that the machine is ready for the next job.

**Topic:** `*-done` (e.g., `dough-machine-done`)

| Field | Type | Description |
| :--- | :--- | :--- |
| `pizzaId` | `int` | ID of the pizza just completed. |
| `orderId` | `string` | ID of the order. |
| `doneMsg` | `boolean` | Always `true`, signals completion. |

**Example:**

```json
{
  "pizzaId": 42,
  "orderId": "0d956eaa-5cc8-4320-b62c-3ca8249085af",
  "doneMsg": true
}
```

### 3\. Order Processing & Done Messages (Order Flow Tracking)

These simple messages mark the start and end of an entire order, used primarily for end-to-end latency measurement.

| Message Type | Topic | Example |
| :--- | :--- | :--- |
| **Order Processing (Start)** | `order-processing` | `{"orderId": "0d956eaa-5cc8-4320-b62c-3ca8249085af", "orderSize": 3, "startTimestamp": 1731571200000}` |
| **Order Done (End)** | `order-done` | `{"orderId": "0d956eaa-5cc8-4320-b62c-3ca8249085af", "endTimestamp": 1731571380000}` |

### 4\. Pizza Done Message (Final Latency Record)

Sent to the dedicated `pizza-done` topic by the **Packaging-robot** to finalize the record for an individual pizza's total time.

| Field | Type | Description |
| :--- | :--- | :--- |
| `orderId` | `string` | ID of the order. |
| `orderSize` | `int` | Total number of pizzas in the order. |
| `pizzaId` | `int` | Unique ID for the pizza. |
| `endTimestamp` | `long` | The final completion timestamp for this pizza. |
| `sauce` | `string` | Sauce type applied. |
| `cheese`, `meat`, `veggies` | `array<string>` | Toppings applied. |
| `baked` | `boolean` | Whether the pizza has been baked. |

**Example:**

```json
{
  "orderId": "0d956eaa-5cc8-4320-b62c-3ca8249085af",
  "orderSize": 3,
  "pizzaId": 1,
  "endTimestamp": 1731571380000,
  "sauce": "tomato",
  "cheese": ["mozzarella"],
  "meat": ["pepperoni", "sausage"],
  "veggies": ["mushroom", "onion"],
  "baked": true
}
```

### 5\. Restock Request Message

This message is sent by a production machine when its internal stock of one or more ingredients is running low. It is intended for a simulated **Internal Goods Provider** to track restocking needs and latency.

| Field | Type | Description |
| :--- | :--- | :--- |
| `machineId` | `string` | The ID of the machine requesting the restock (e.g., `"cheese-machine"`). |
| `items` | `array<object>` | List of items that need restocking. |
| `items[].itemType` | `string` | The specific ingredient type (e.g., `"mozzarella"`). |
| `items[].currentStock` | `int` | The current stock level of the item. |
| `items[].requestedAmount`| `int` | The amount requested in the restock order. |
| `requestTimestamp` | `long` | Timestamp when the request was made. |

**Example:**

```json
{
  "machineId": "cheese-machine",
  "items": [
    {
      "itemType": "mozzarella",
      "currentStock": 10,
      "requestedAmount": 90
    },
    {
      "itemType": "gorgonzola",
      "currentStock": 17,
      "requestedAmount": 83
    }
  ],
  "requestTimestamp": 1731571200000
}
```

### 6\. Restock Done Message

This message is sent by the **Internal Goods Provider** (simulated) upon the completion of a restock delivery to the requesting machine. It is used to measure the **Restock Latency** (from `requestTimestamp` to `completedTimestamp`).

**Topic:** `restock-done`

| Field | Type | Description |
| :--- | :--- | :--- |
| `machineId` | `string` | The ID of the machine that received the restock. |
| `items` | `array<object>` | List of items delivered. |
| `items[].itemType` | `string` | The specific ingredient type. |
| `items[].deliveredAmount` | `int` | The amount of the item delivered. |
| `completedTimestamp` | `long` | Timestamp when the restock was completed at the machine. |

**Example:**

```json
{
  "machineId": "cheese-machine",
  "items": [
    {
      "itemType": "mozzarella",
      "deliveredAmount": 90
    },
    {
      "itemType": "gorgonzola",
      "deliveredAmount": 83
    }
  ],
  "completedTimestamp": 1731571260000
}
```

### 7\. Order Dispatched Message

Sent by the **OrderProcessing** service when an order has been produced and dispatched to the warehouse to be stored. This message is useful for measuring the latency between order start and dispatch.

**Topic:** `order-dispatched`

| Field | Type | Description |
| :--- | :--- | :--- |
| `orderId` | `string` | Unique ID of the customer order (UUID is suggested for robustness). |
| `orderSize` | `int` | Total number of pizzas in the order. |
| `msgDesc` | `string` | Description of the dispatch event. |
| `dispatchedTimestamp` | `long` | Timestamp when the order was dispatched. |

**Example:**

```json
{
    "orderId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    "orderSize": 10,
    "msgDesc": "10 pizzas dispatched to warehouse",
    "dispatchedTimestamp": 1731571230000
}
```

## KSQLDB: Real-Time Observability

This section defines the KSQLDB statements used to calculate and monitor end-to-end order and individual pizza latency by joining the **start** and **done** messages from the Kafka topics.

### 1\. KSQL Streams and Tables Definitions

These statements create the necessary streams (raw event sources) and tables (stateful views used for joins and aggregation) from the Kafka topics.

### 2\. Latency Calculation Tables

These tables perform a `LEFT JOIN` on the start and end time tables to dynamically calculate the total latency (`latencyMs`).

### 3\. Monitoring Queries & Examples

Use these queries in Redpanda Console or the KSQLDB CLI to monitor latency in real-time.

| Measurement | KSQLDB Query |
| :--- | :--- |
| **Order Latency** | `SELECT * FROM order_latency EMIT CHANGES;` |
| **Pizza Latency** | `SELECT * FROM pizza_latency EMIT CHANGES;` |

**Example Output (Order Latency):**

```sql
SELECT * FROM order_latency EMIT CHANGES;
+---------------+---------------+---------------+---------------+---------------+
|ORDERID        |ORDERSIZE      |STARTTIMESTAMP |ENDTIMESTAMP   |LATENCYMS      |
+---------------+---------------+---------------+---------------+---------------+
|0d956eaa-5cc8-4320-b62c-3ca8249085af            |2              |1763312501613  |null           |null           |
|0d956eaa-5cc8-4320-b62c-3ca8249085af            |2              |1763312501613  |1763312520802  |19189          |
```

**Example Output (Pizza Latency)**
```sql
SELECT * FROM pizza_latency;
+---------------------------+---------------------------+---------------------------+---------------------------+---------------------------+---------------------------+
|S_PIZZA_ORDER_KEY          |S_PIZZA_ID                 |S_ORDER_ID                 |STARTTIMESTAMP             |ENDTIMESTAMP               |LATENCYMS                  |
+---------------------------+---------------------------+---------------------------+---------------------------+---------------------------+---------------------------+
|1_363ef34f-4e0b-4058-95fe-52899ebaac7b                      |1                          |363ef34f-4e0b-4058-95fe-52899ebaac7b                        |1763367145274              |1763367162756              |17482                      |
|1_363ef34f-4e0b-4058-95fe-52899ebaac7b                      |1                          |363ef34f-4e0b-4058-95fe-52899ebaac7b                        |1763367138905              |1763367154218              |15313                      |
|2_363ef34f-4e0b-4058-95fe-52899ebaac7b                      |2                          |363ef34f-4e0b-4058-95fe-52899ebaac7b                        |1763367145274              |1763367166045              |20771                      |
|2_363ef34f-4e0b-4058-95fe-52899ebaac7b                      |2                          |363ef34f-4e0b-4058-95fe-52899ebaac7b                        |1763367138905              |1763367156896              |17991                      |
```

### 4\. Restock Latency Calculation

To calculate the time it takes for an internal restock, KSQLDB can join the `restock-request` and `restock-done` streams based on the `machineId`.

Measure Restock Latency (`completedTimestamp - requestTimestamp`). This can be further aggregated to measure the average restocking time against total production time.

### 5\. Order Dispatch Latency Calculation

Measure the delay between an order starting (`order-processing` topic) and the last pizza produced being dispatched (`order-dispatched` topic). This is key for measuring the whole process from order receipt to warehouse dispatch.


## Data Export API (FastAPI)

The production data from KSQLDB is exposed via a FastAPI service, allowing you to easily query and extract the latency tables in real-time using standard HTTP requests.

### Base URL

When running locally with Docker Compose, the API service is typically accessible on port `8000` (or as defined in your `docker-compose.yml`).

```
http://localhost:8000/ksql/
```

### Available Endpoints

Both endpoints support the `order_latency` and `pizza_latency` tables. The optional `limit` parameter controls the number of records returned.

#### 1\. Get Table as CSV

Retrieves the data as plain text with the correct `text/csv` header, making it easy to copy/paste or pipe the output into files or other tools.

```
GET /ksql/{table_name}/csv?limit={n}
```

| Parameter | Required/Optional | Description |
| :--- | :--- | :--- |
| `table_name` | **Required** | The KSQLDB table to query: `order_latency` or `pizza_latency`. |
| `limit` | Optional (Default: 100) | Maximum number of rows to return. |

**Example Request:**

```
GET http://localhost:8000/ksql/order_latency/
```

**Response (CSV Content):**

```csv
ORDERID,ORDERSIZE,STARTTIMESTAMP,ENDTIMESTAMP,LATENCYMS
10,5,1763308900000,1763308950000,50000
11,10,1763308910000,1763308975000,65000
```


## Data Tables Description

This data is sourced directly from the KSQLDB tables defined previously.

### **Order Latency Table (`order_latency`)**

| Column | Description |
| :--- | :--- |
| `ORDERID` | Unique ID of the customer order |
| `ORDERSIZE` | Number of pizzas in the order |
| `STARTTIMESTAMP` | Timestamp when order processing started |
| `ENDTIMESTAMP` | Timestamp when order completed |
| `LATENCYMS` | Total processing time in milliseconds |

### **Pizza Latency Table (`pizza_latency`)**

| Column | Description |
| :--- | :--- |
| `PIZZAID` | Unique ID of the pizza |
| `ORDERID` | ID of the order this pizza belongs to |
| `STARTTIMESTAMP` | Timestamp when pizza processing started |
| `ENDTIMESTAMP` | Timestamp when pizza completed |
| `LATENCYMS` | Total processing time in milliseconds |

> TO REVIEW IF WE HAVE TO ADD MORE TABLES AND ENDPOINT TO EXPORT DATA
