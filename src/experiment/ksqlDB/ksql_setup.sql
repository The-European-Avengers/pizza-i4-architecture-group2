-- -----------------------------
-- STREAMS
-- -----------------------------

-- Stream crudo de pasos de pizza
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

-- Streams de órdenes
CREATE STREAM order_processing_stream (
    orderId INT,
    orderSize INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-processing',
    VALUE_FORMAT='JSON'
);

CREATE STREAM order_done_stream (
    orderId INT,
    endTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='order-done',
    VALUE_FORMAT='JSON'
);

-- Stream de dough machine
CREATE STREAM dough_stream (
    pizzaId INT,
    orderId INT,
    startTimestamp BIGINT
) WITH (
    KAFKA_TOPIC='dough-machine',
    VALUE_FORMAT='JSON'
);

-- Stream rekeyed por pizzaId
CREATE STREAM dough_stream_rekeyed AS
SELECT
    pizzaId,
    orderId,
    startTimestamp
FROM dough_stream
PARTITION BY pizzaId;

-- -----------------------------
-- TABLAS
-- -----------------------------

-- Tabla de ordenes: start (CON orderSize)
CREATE TABLE order_processing_table AS
SELECT
    orderId,
    LATEST_BY_OFFSET(orderSize) AS ORDER_SIZE,
    MIN(startTimestamp) AS start_ts
FROM order_processing_stream
GROUP BY orderId
EMIT CHANGES;

-- Tabla de ordenes: end
CREATE TABLE order_done_table AS
SELECT
    orderId,
    MAX(endTimestamp) AS end_ts
FROM order_done_stream
GROUP BY orderId
EMIT CHANGES;

-- Tabla latencia por orden (SIMPLIFICADA - sin order_size_table)
CREATE TABLE order_latency AS
SELECT
    p.orderId AS orderId,
    p.ORDER_SIZE AS orderSize,     -- ✅ Viene directamente de order_processing_table
    p.start_ts AS startTimestamp,
    d.end_ts AS endTimestamp,
    (d.end_ts - p.start_ts) AS latencyMs
FROM order_processing_table p
LEFT JOIN order_done_table d
    ON p.orderId = d.orderId
EMIT CHANGES;

-- Tabla de inicio de pizza
CREATE TABLE pizza_start_table AS
SELECT
    pizzaId,
    LATEST_BY_OFFSET(orderId) AS orderId,
    MIN(startTimestamp) AS startTimestamp
FROM dough_stream_rekeyed
GROUP BY pizzaId
EMIT CHANGES;

-- Tabla de fin de pizza
CREATE TABLE pizza_end_table AS
SELECT
    pizzaId,
    LATEST_BY_OFFSET(orderId) AS orderId,
    MAX(endTimestamp) AS endTimestamp
FROM pizza_steps_raw
GROUP BY pizzaId
EMIT CHANGES;

-- Tabla latencia por pizza
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