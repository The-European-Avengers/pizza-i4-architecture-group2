-- -----------------------------
-- STREAMS
-- -----------------------------

-- Raw pizza steps stream (Final completion events)
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


-- Order processing start stream
CREATE STREAM order_processing_stream (
    orderId INT,
    orderSize INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-processing',
    VALUE_FORMAT='JSON'
);

-- Order completion stream
CREATE STREAM order_done_stream (
    orderId INT,
    endTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-done',
    VALUE_FORMAT='JSON'
);


-- Dough machine stream (First step of pizza processing)
CREATE STREAM dough_stream (
    pizzaId INT,
    orderId INT,
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
    CAST(pizzaId AS VARCHAR) + '_' + CAST(orderId AS VARCHAR) AS PIZZA_ORDER_KEY,
    pizzaId,
    orderId,
    -- Renaming the field that holds the Order's start time
    startTimestamp AS ORDER_START_TIME,
    -- ROWTIME (the event's timestamp) is the accurate Pizza Start Time
    ROWTIME AS PIZZA_START_TIME
FROM dough_stream
PARTITION BY CAST(pizzaId AS VARCHAR) + '_' + CAST(orderId AS VARCHAR);

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
    CAST(pizzaId AS VARCHAR) + '_' + CAST(orderId AS VARCHAR) AS PIZZA_ORDER_KEY,
    pizzaId,
    orderId,
    endTimestamp
FROM pizza_steps_raw
PARTITION BY CAST(pizzaId AS VARCHAR) + '_' + CAST(orderId AS VARCHAR);

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


-- -----------------------------
-- RESTOCKING STREAMS & TABLES
-- -----------------------------

-- Restock request stream
CREATE STREAM restock_request_stream (
    machineId VARCHAR,
    items ARRAY<STRUCT<itemType VARCHAR, currentStock INT, requestedAmount INT>>,
    requestTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='*-restock',
    VALUE_FORMAT='JSON'
);

-- Restock done stream
CREATE STREAM restock_done_stream (
    machineId VARCHAR,
    items ARRAY<STRUCT<itemType VARCHAR, deliveredAmount INT>>,
    completedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='*-restock-done',
    VALUE_FORMAT='JSON'
);


-- Example: Explode restock items for latency calculation
CREATE STREAM restock_request_exploded AS
SELECT
    machineId,
    EXPLODE(items) AS item,
    requestTimestamp
FROM restock_request_stream;

CREATE STREAM restock_done_exploded AS
SELECT
    machineId,
    EXPLODE(items) AS item,
    completedTimestamp
FROM restock_done_stream;

-- Restock latency table
CREATE TABLE restock_latency AS
SELECT
    r.machineId,
    r.item->itemType AS itemType,
    r.requestTimestamp,
    d.completedTimestamp,
    (d.completedTimestamp - r.requestTimestamp) AS latencyMs
FROM restock_request_exploded r
LEFT JOIN restock_done_exploded d
    ON r.machineId = d.machineId
   AND r.item->itemType = d.item->itemType
EMIT CHANGES;


CREATE STREAM order_dispatched_stream (
    orderId VARCHAR,
    orderSize INT,
    msgDesc VARCHAR,
    dispatchedTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-dispatched',
    VALUE_FORMAT='JSON'
);


CREATE TABLE order_dispatch_latency AS
SELECT
    p.orderId,
    p.ORDER_SIZE,
    p.start_ts AS startTimestamp,
    d.dispatchedTimestamp AS dispatchedTimestamp,
    (d.dispatchedTimestamp - p.start_ts) AS latencyMs
FROM order_processing_table p
LEFT JOIN order_dispatched_stream d
    ON p.orderId = d.orderId
EMIT CHANGES;


CREATE TABLE avg_restock_latency AS
SELECT machineId,
       AVG(latencyMs) AS avgLatency
FROM restock_latency
GROUP BY machineId
EMIT CHANGES;
