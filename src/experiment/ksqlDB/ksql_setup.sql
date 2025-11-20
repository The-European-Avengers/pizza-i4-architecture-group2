-- -----------------------------
-- STREAMS
-- -----------------------------

-- Raw pizza steps stream (Final completion events)
CREATE STREAM pizza_steps_raw (
    pizzaId INT,
    orderId VARCHAR,
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


-- Order processing start stream
CREATE STREAM order_processing_stream (
    orderId VARCHAR,
    orderSize INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-processing',
    VALUE_FORMAT='JSON'
);

-- Order completion stream
CREATE STREAM order_done_stream (
    orderId VARCHAR,
    endTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-done',
    VALUE_FORMAT='JSON'
);

-- Order processing start stream
CREATE STREAM order_stack_stream (
    orderId VARCHAR,
    orderSize INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-stack',
    VALUE_FORMAT='JSON'
);

-- Order completion stream
CREATE STREAM order_dispatched_stream (
    orderId VARCHAR,
    endTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-dispatched',
    VALUE_FORMAT='JSON'
);


-- Dough machine stream (First step of pizza processing)
CREATE STREAM dough_stream (
    pizzaId INT,
    orderId VARCHAR,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='dough-machine',
    VALUE_FORMAT='JSON'
);

-- Stream rekeyed for pizza start events using the COMPOSITE KEY: (pizzaId, orderId)
-- Kafka's ROWTIME is used as the actual pizza start time for accurate latency calculation.
CREATE STREAM dough_stream_rekeyed AS
SELECT
    -- Create the composite key by combining the IDs
    CAST(pizzaId AS VARCHAR) + '_' + orderId AS PIZZA_ORDER_KEY,
    pizzaId,
    orderId,
    -- Renaming the field that holds the Order's start time
    startTimestamp AS ORDER_START_TIME,
    -- ROWTIME (the event's timestamp) is the accurate Pizza Start Time
    ROWTIME AS PIZZA_START_TIME
FROM dough_stream
PARTITION BY (CAST(pizzaId AS VARCHAR) + '_' + orderId);

-- NOTE: Order streams and tables (order_processing_stream, order_done_stream, etc.) are omitted here
-- but rely on being correctly grouped by orderId only.

-- -----------------------------
-- TABLES
-- -----------------------------

-- Order Table: Tracks the consolidated start time and size of an order
CREATE TABLE order_processing_table AS
SELECT
    orderId,
    LATEST_BY_OFFSET(orderSize) AS ORDER_SIZE,
    MIN(startTimestamp) AS start_ts
FROM order_processing_stream
GROUP BY orderId
EMIT CHANGES;

-- Order Table: Tracks the consolidated end time of an order
CREATE TABLE order_done_table AS
SELECT
    orderId,
    MAX(endTimestamp) AS end_ts
FROM order_done_stream
GROUP BY orderId
EMIT CHANGES;

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


-- Table: Pizza start time, grouped by PIZZA_ORDER_KEY
-- Uses PIZZA_START_TIME (ROWTIME) for the individual pizza's start time.
CREATE TABLE pizza_start_table AS
SELECT
    PIZZA_ORDER_KEY,
    LATEST_BY_OFFSET(pizzaId) AS PIZZA_ID,
    LATEST_BY_OFFSET(orderId) AS ORDER_ID,
    -- Using the calculated PIZZA_START_TIME (from ROWTIME)
    MIN(PIZZA_START_TIME) AS STARTTIMESTAMP 
FROM dough_stream_rekeyed
GROUP BY PIZZA_ORDER_KEY
EMIT CHANGES;

-- Stream rekeyed for end events (pizza_steps_raw)
CREATE STREAM pizza_steps_raw_rekeyed AS
SELECT
    CAST(pizzaId AS VARCHAR) + '_' + orderId AS PIZZA_ORDER_KEY,
    pizzaId,
    orderId,
    endTimestamp
FROM pizza_steps_raw
PARTITION BY (CAST(pizzaId AS VARCHAR) + '_' + orderId);

-- Pizza end table: Grouped by PIZZA_ORDER_KEY
CREATE TABLE pizza_end_table AS
SELECT
    PIZZA_ORDER_KEY,
    LATEST_BY_OFFSET(pizzaId) AS PIZZA_ID,
    LATEST_BY_OFFSET(orderId) AS ORDER_ID,
    MAX(endTimestamp) AS ENDTIMESTAMP
FROM pizza_steps_raw_rekeyed
GROUP BY PIZZA_ORDER_KEY
EMIT CHANGES;

-- Table: End-to-end latency for each individual pizza, joined by PIZZA_ORDER_KEY
CREATE TABLE pizza_latency AS
SELECT
    s.PIZZA_ORDER_KEY,
    s.PIZZA_ID,
    s.ORDER_ID,
    s.STARTTIMESTAMP, -- Now the accurate pizza start time
    e.ENDTIMESTAMP,
    (e.ENDTIMESTAMP - s.STARTTIMESTAMP) AS LATENCYMS
FROM pizza_start_table s
LEFT JOIN pizza_end_table e
    ON s.PIZZA_ORDER_KEY = e.PIZZA_ORDER_KEY
EMIT CHANGES;


CREATE TABLE order_stack_table AS
SELECT
    orderId,
    LATEST_BY_OFFSET(orderSize) AS orderSize,
    MIN(startTimestamp) AS start_ts
FROM order_stack_stream
GROUP BY orderId
EMIT CHANGES;


CREATE TABLE order_dispatched_table AS
SELECT
    orderId,
    MAX(endTimestamp) AS end_ts
FROM order_dispatched_stream
GROUP BY orderId
EMIT CHANGES;


CREATE TABLE order_dispatch_latency AS
SELECT
    s.orderId AS orderId,
    s.orderSize AS orderSize,
    s.start_ts AS startTimestamp,
    d.end_ts AS endTimestamp,
    (d.end_ts - s.start_ts) AS latencyMs
FROM order_stack_table s
LEFT JOIN order_dispatched_table d
    ON s.orderId = d.orderId
EMIT CHANGES;

-- ----------------------------------------------------
-- RESTOCK REQUEST STREAM
-- ----------------------------------------------------
--------------------------------------------------------------------------------
-- DOUGH MACHINE
--------------------------------------------------------------------------------

CREATE STREAM dough_machine_restock (
    machineId VARCHAR KEY,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='dough-machine-restock',
    VALUE_FORMAT='JSON'
);

CREATE STREAM dough_machine_restock_done (
    machineId VARCHAR KEY,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='dough-machine-restock-done',
    VALUE_FORMAT='JSON'
);

CREATE TABLE dough_machine_restock_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(requestTimestamp) AS requestTimestamp
FROM dough_machine_restock
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE dough_machine_restock_done_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(completedTimestamp) AS completedTimestamp
FROM dough_machine_restock_done
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE dough_machine_restock_latency AS
SELECT
    r.machineId AS machineId,
    (d.completedTimestamp - r.requestTimestamp) AS restockLatencyMs
FROM dough_machine_restock_t r
JOIN dough_machine_restock_done_t d
ON r.machineId = d.machineId
EMIT CHANGES;

--------------------------------------------------------------------------------
-- SAUCE MACHINE
--------------------------------------------------------------------------------

CREATE STREAM sauce_machine_restock (
    machineId VARCHAR KEY,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='sauce-machine-restock',
    VALUE_FORMAT='JSON'
);

CREATE STREAM sauce_machine_restock_done (
    machineId VARCHAR KEY,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='sauce-machine-restock-done',
    VALUE_FORMAT='JSON'
);

CREATE TABLE sauce_machine_restock_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(requestTimestamp) AS requestTimestamp
FROM sauce_machine_restock
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE sauce_machine_restock_done_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(completedTimestamp) AS completedTimestamp
FROM sauce_machine_restock_done
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE sauce_machine_restock_latency AS
SELECT
    r.machineId AS machineId,
    (d.completedTimestamp - r.requestTimestamp) AS restockLatencyMs
FROM sauce_machine_restock_t r
JOIN sauce_machine_restock_done_t d
ON r.machineId = d.machineId
EMIT CHANGES;

--------------------------------------------------------------------------------
-- CHEESE MACHINE
--------------------------------------------------------------------------------

CREATE STREAM cheese_machine_restock (
    machineId VARCHAR KEY,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='cheese-machine-restock',
    VALUE_FORMAT='JSON'
);

CREATE STREAM cheese_machine_restock_done (
    machineId VARCHAR KEY,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='cheese-machine-restock-done',
    VALUE_FORMAT='JSON'
);

CREATE TABLE cheese_machine_restock_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(requestTimestamp) AS requestTimestamp
FROM cheese_machine_restock
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE cheese_machine_restock_done_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(completedTimestamp) AS completedTimestamp
FROM cheese_machine_restock_done
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE cheese_machine_restock_latency AS
SELECT
    r.machineId AS machineId,
    (d.completedTimestamp - r.requestTimestamp) AS restockLatencyMs
FROM cheese_machine_restock_t r
JOIN cheese_machine_restock_done_t d
ON r.machineId = d.machineId
EMIT CHANGES;

--------------------------------------------------------------------------------
-- MEAT MACHINE
--------------------------------------------------------------------------------

CREATE STREAM meat_machine_restock (
    machineId VARCHAR KEY,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='meat-machine-restock',
    VALUE_FORMAT='JSON'
);

CREATE STREAM meat_machine_restock_done (
    machineId VARCHAR KEY,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='meat-machine-restock-done',
    VALUE_FORMAT='JSON'
);

CREATE TABLE meat_machine_restock_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(requestTimestamp) AS requestTimestamp
FROM meat_machine_restock
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE meat_machine_restock_done_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(completedTimestamp) AS completedTimestamp
FROM meat_machine_restock_done
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE meat_machine_restock_latency AS
SELECT
    r.machineId AS machineId,
    (d.completedTimestamp - r.requestTimestamp) AS restockLatencyMs
FROM meat_machine_restock_t r
JOIN meat_machine_restock_done_t d
ON r.machineId = d.machineId
EMIT CHANGES;

--------------------------------------------------------------------------------
-- VEGETABLES MACHINE
--------------------------------------------------------------------------------

CREATE STREAM vegetables_machine_restock (
    machineId VARCHAR KEY,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='vegetables-machine-restock',
    VALUE_FORMAT='JSON'
);

CREATE STREAM vegetables_machine_restock_done (
    machineId VARCHAR KEY,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='vegetables-machine-restock-done',
    VALUE_FORMAT='JSON'
);

CREATE TABLE vegetables_machine_restock_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(requestTimestamp) AS requestTimestamp
FROM vegetables_machine_restock
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE vegetables_machine_restock_done_t AS
SELECT 
       machineId,
       LATEST_BY_OFFSET(completedTimestamp) AS completedTimestamp
FROM vegetables_machine_restock_done
GROUP BY machineId
EMIT CHANGES;

CREATE TABLE vegetables_machine_restock_latency AS
SELECT
    r.machineId AS machineId,
    (d.completedTimestamp - r.requestTimestamp) AS restockLatencyMs
FROM vegetables_machine_restock_t r
JOIN vegetables_machine_restock_done_t d
ON r.machineId = d.machineId
EMIT CHANGES;

--------------------------------------------------------------------------------
-- PACKAGING MACHINE
--------------------------------------------------------------------------------

CREATE STREAM packaging_machine_restock (
    machine VARCHAR KEY,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='packaging-machine-restock',
    VALUE_FORMAT='JSON'
);

CREATE STREAM packaging_machine_restock_done (
    machine VARCHAR KEY,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='packaging-machine-restock-done',
    VALUE_FORMAT='JSON'
);

CREATE TABLE packaging_machine_restock_t AS
SELECT 
       machine,
       LATEST_BY_OFFSET(requestTimestamp) AS requestTimestamp
FROM packaging_machine_restock
GROUP BY machine
EMIT CHANGES;

CREATE TABLE packaging_machine_restock_done_t AS
SELECT 
       machine,
       LATEST_BY_OFFSET(completedTimestamp) AS completedTimestamp
FROM packaging_machine_restock_done
GROUP BY machine
EMIT CHANGES;

CREATE TABLE packaging_machine_restock_latency AS
SELECT
    r.machine AS machine,
    (d.completedTimestamp - r.requestTimestamp) AS restockLatencyMs
FROM packaging_machine_restock_t r
JOIN packaging_machine_restock_done_t d
ON r.machine = d.machine
EMIT CHANGES;