# Production Line Experiment

## Quick Start

### 1. Build and Run All Services

All services (Kafka, C# machines, Java machines) are containerized. Run the following command from the root experiment folder (where your `docker-compose.yml` is).

```bash
docker-compose up --build
```

**What to Expect:**
You will first see the `kafka-init` service start. It will connect to Kafka, create all the necessary topics (e.g., `dough-machine`, `dough-machine-done`), and then exit successfully. Once it is finished, docker-compose will automatically start all the other services (`order-processor`, `dough-machine`, etc.).

Wait for all services to log that they are "subscribed" and "ready."

### 2. Monitor Kafka (Optional)

To monitor Kafka topics in real-time, open Redpanda Console in your browser:

```
http://localhost:8090/
```

### 3. Run an Order

To simulate a customer order, run any of the following commands in a new terminal:

Start an order for 10 pizzas:

```bash
curl -X POST http://localhost:8082/start-order/10
```

Start an order for 50 pizzas:

```bash
curl -X POST http://localhost:8082/start-order/50
```

Start an order for 100 pizzas:

```bash
curl -X POST http://localhost:8082/start-order/100
```

You can watch the logs in your docker-compose terminal to see the entire production line process the order one pizza at a time.

### 4. Run a New Experiment

This is a critical step. Kafka saves all your messages to a data volume. If you just restart the services, the order-processor will re-read old "done" messages from the last run.

To run a clean, new experiment, you must first stop and delete the containers AND the Kafka data volume:

```bash
docker-compose down -v
```

After this command finishes, you can go back to Step 1 (`docker-compose up --build`) to start a fresh run.

## Folder Structure

Each service lives in its own folder and includes a dedicated `Dockerfile`.
To experiment, we intentionally implemented the production line using multiple programming languages.

### C#

- **KafkaInitializer**: Bootstraps the system by creating all required Kafka topics.
- **OrderProcessing**: Manages incoming orders and dispatches pizzas to the first machine in the production line.
- **DoughMachine**: Simulates the preparation of raw dough.
- **DoughShaper**: Simulates shaping the dough into pizza bases.

### Java

- **CheeseGrater**: Simulates grating and adding cheese to the pizza.

### Python

- **SauceMachine**: Chooses and applies the correct sauce to the pizza.
- **Oven**: Handles the baking process.
- **Freezer**: Handles the freezing step for pizzas that require freezing.

### Go

- **MeatSlicer**: Selects and applies meat toppings.
- **VegetablesSlicer**: Selects and applies vegetable toppings.
- **Packaging-robot**: Packages finished pizzas into boxes.

## Kafka Topics

These are all Kafka topics used by the system:

- `dough-machine` / `dough-machine-done`
- `dough-shaper` / `dough-shaper-done`
- `sauce-machine` / `sauce-machine-done`
- `cheese-machine` / `cheese-machine-done`
- `meat-machine` / `meat-machine-done`
- `vegetables-machine` / `vegetables-machine-done`
- `freezer-machine` / `freezer-machine-done`
- `oven-machine` / `oven-machine-done`
- `packaging-machine` / `packaging-machine-done`
- `pizza-done`
- `order-stack`
- `order-processing`
- `order-done`

## Production Line Topics

Each machine communicates exclusively through Kafka.
A machine **consumes** from its assigned topic and **produces** to the next machine’s topic.
Additionally, every machine has a `-done` topic that signals it is ready for the next pizza.

- `dough-machine`: Notifies the dough machine about the next pizza to prepare.
- `dough-machine-done`: Emitted when the dough machine finishes its task.

The same structure applies to all other machines:

- `dough-shaper` / `dough-shaper-done`
- `sauce-machine` / `sauce-machine-done`
- `cheese-machine` / `cheese-machine-done`
- `meat-machine` / `meat-machine-done`
- `vegetables-machine` / `vegetables-machine-done`
- `freezer-machine` / `freezer-machine-done`
- `oven-machine` / `oven-machine-done`
- `packaging-machine` / `packaging-machine-done`

## Order Tracking Topics

- **`pizza-done`**: Produced by the packaging machine to record when a pizza is completed. Used to measure production time.
- **`order-stack`**: Queue of orders waiting to be processed. When multiple orders arrive, they are stored here and processed sequentially. Future enhancements include priority-aware ordering.
- **`order-processing`**: Emitted when an order starts processing, including its start timestamp.
- **`order-done`**: Emitted when all pizzas for an order are completed, including the completion timestamp.

## Kafka Messages

The following section describes all Kafka message types used throughout the pizza production workflow, detailing the structure and purpose of each message exchanged between services.

### Pizza Order Message

Describes the state of a single pizza and travels through all machines.

- `pizzaId` (int): Unique ID for the pizza.
- `orderId` (int): ID of the order this pizza belongs to.
- `orderSize` (int): Total number of pizzas in the order.
- `startTimestamp` (long): When order processing began.
- `endTimestamp` (long): When order processing finished (nullable until complete).
- `msgDesc` (string): Description of the current processing step.
- `sauce` (string): Sauce type.
- `baked` (boolean): Whether the pizza has been baked.
- `cheese`, `meat`, `veggies` (array of strings): Toppings applied so far.

**Example:**

```json
{
  "pizzaId": 42,
  "orderId": 123,
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

### Step Done Message

Sent by a machine after finishing a task to signal readiness for the next pizza.

- `pizzaId`
- `orderId`
- `doneMsg` (boolean)

**Example:**

```json
{
  "pizzaId": 42,
  "orderId": 123,
  "doneMsg": true
}
```

### Order Processing Message

Emitted to `order-processing` when an order starts:

```json
{
  "orderId": 123,
  "orderSize": 3,
  "startTimestamp": 1731571200000
}
```

### Order Done Message

Emitted to `order-done` when all pizzas for the order are complete:

```json
{
  "orderId": 123,
  "endTimestamp": 1731571380000
}
```

### Order Stack Message

Used to store pending orders in `order-stack` (future enhancement):

```json
{
  "orderId": 123,
  "priority": 1,
  "orderSize": 3,
  "pizzas": [
    {
      "pizzaId": 1,
      "sauce": "tomato",
      "cheese": ["mozzarella"],
      "meat": ["pepperoni"],
      "veggies": ["mushroom"]
    },
    {
      "pizzaId": 2,
      "sauce": "pesto",
      "cheese": ["mozzarella", "parmesan"],
      "meat": [],
      "veggies": ["basil", "garlic"]
    },
    {
      "pizzaId": 3,
      "sauce": "tomato",
      "cheese": ["mozzarella"],
      "meat": ["sausage"],
      "veggies": ["onion", "bell pepper"]
    }
  ]
}
```

### Pizza Done Message

Sent to `pizza-done` every time an individual pizza is completed:

- `orderId`
- `orderSize`
- `pizzaId`
- `endTimestamp`

**Example:**

```json
{
  "orderId": 123,
  "orderSize": 3,
  "pizzaId": 1,
  "endTimestamp": 1731571380000
}
```

## KSQLDB

This section defines the KSQLDB statements used to calculate and monitor end-to-end order and individual pizza latency by joining the **start** and **done** messages from the Kafka topics.

### 1\. Streams and Tables Definitions

These definitions create the necessary streams (raw event sources) and tables (stateful views used for joins) from the Kafka topics.

```sql
-- -----------------------------
-- STREAMS
-- -----------------------------

-- Stream of completed pizza messages from the 'pizza-done' topic
CREATE STREAM pizza_steps_raw (
    pizzaId INT,
    orderId INT,
    orderSize INT,
    startTimestamp BIGINT,
    endTimestamp BIGINT,
    msgDesc VARCHAR,
    sauce VARCHAR,
    baked BOOLEAN,
    cheese ARRAY<VARCHAR>,
    meat ARRAY<VARCHAR>,
    veggies ARRAY<VARCHAR>
) WITH (
    KAFKA_TOPIC='pizza-done',
    VALUE_FORMAT='JSON'
);

-- Stream for when an order starts processing
CREATE STREAM order_processing_stream (
    orderId INT,
    orderSize INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-processing',
    VALUE_FORMAT='JSON'
);

-- Stream for when an order is completed
CREATE STREAM order_done_stream (
    orderId INT,
    endTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-done',
    VALUE_FORMAT='JSON'
);

-- Stream for the very first step of a pizza (Dough Machine)
CREATE STREAM dough_stream (
    pizzaId INT,
    orderId INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='dough-machine',
    VALUE_FORMAT='JSON'
);

-- Stream rekeyed by pizzaId for efficient joins
CREATE STREAM dough_stream_rekeyed AS
SELECT
    pizzaId,
    orderId,
    startTimestamp
FROM dough_stream
PARTITION BY pizzaId;

-- -----------------------------
-- TABLES (Aggregated State for Joins)
-- -----------------------------

-- Table to track the start time and size of an order
CREATE TABLE order_processing_table AS
SELECT
    orderId,
    LATEST_BY_OFFSET(orderSize) AS ORDER_SIZE,
    MIN(startTimestamp) AS start_ts
FROM order_processing_stream
GROUP BY orderId
EMIT CHANGES;

-- Table to track the completion time of an order
CREATE TABLE order_done_table AS
SELECT
    orderId,
    MAX(endTimestamp) AS end_ts
FROM order_done_stream
GROUP BY orderId
EMIT CHANGES;

-- Table to track the start time of each individual pizza
CREATE TABLE pizza_start_table AS
SELECT
    pizzaId,
    LATEST_BY_OFFSET(orderId) AS orderId,
    MIN(startTimestamp) AS startTimestamp
FROM dough_stream_rekeyed
GROUP BY pizzaId
EMIT CHANGES;

-- Table to track the completion time of each individual pizza
CREATE TABLE pizza_end_table AS
SELECT
    pizzaId,
    LATEST_BY_OFFSET(orderId) AS orderId,
    MAX(endTimestamp) AS endTimestamp
FROM pizza_steps_raw
GROUP BY pizzaId
EMIT CHANGES;
```

-----

### 2\. Latency Calculation Tables

These tables join the start and end timestamps to calculate the total latency for both orders and individual pizzas.

```sql
-- Table: End-to-end latency for each order
CREATE TABLE order_latency AS
SELECT
    p.orderId AS orderId,
    p.ORDER_SIZE AS orderSize,
    p.start_ts AS startTimestamp,
    d.end_ts AS endTimestamp,
    (d.end_ts - p.start_ts) AS latencyMs
FROM order_processing_table p
LEFT JOIN order_done_table d
    ON p.orderId = d.orderId
EMIT CHANGES;

-- Table: End-to-end latency for each individual pizza
CREATE TABLE pizza_latency AS
SELECT
    s.pizzaId,
    s.orderId,
    s.startTimestamp,
    e.endTimestamp,
    (e.endTimestamp - s.startTimestamp) AS latencyMs
FROM pizza_start_table s
LEFT JOIN pizza_end_table e
    ON s.pizzaId = e.pizzaId
EMIT CHANGES;
```

-----

### 3\. Monitoring Queries

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
|349            |2              |1763312501613  |null           |null           |
|349            |2              |1763312501613  |1763312520802  |19189          |
```

**Example Output (Pizza Latency):**

```sql
SELECT * FROM pizza_latency EMIT CHANGES;
+---------------+---------------+---------------+---------------+---------------+
|PIZZAID        |ORDERID        |STARTTIMESTAMP |ENDTIMESTAMP   |LATENCYMS      |
+---------------+---------------+---------------+---------------+---------------+
|1              |349            |1763312501613  |null           |null           |
|2              |349            |1763312501613  |null           |null           |
|1              |349            |1763312501613  |1763312518039  |16426          |
|2              |349            |1763312501613  |1763312520802  |19189          |
```
