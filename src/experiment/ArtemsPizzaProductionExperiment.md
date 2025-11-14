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
curl -X POST http://localhost:8081/start-order/10
```

Start an order for 50 pizzas:
```bash
curl -X POST http://localhost:8081/start-order/50
```

Start an order for 100 pizzas:
```bash
curl -X POST http://localhost:8081/start-order/100
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

Each service must be in its own folder with a `Dockerfile`.
The main production line services are:

- `KafkaInitializer`: A utility service that starts first to create all Kafka topics.
- `OrderProcessingSolution`: Manages orders and sends pizzas to the first machine.
- `DoughMachineSolution`: Simulates preparing the dough.
- `DoughShaperSolution`: Simulates shaping the dough.
- `CheeseGrater`: Simulates grating the cheese (Java service).
- `sauce`: (To be implemented)
- `meat`: (To be implemented)
- `vegetables`: (To be implemented)
- `freezer`: (To be implemented)
- `oven`: (To be implemented)
- `packaging`: (To be implemented)

## Kafka Topics

The production line services communicate via Kafka topics. Each service has its own topic, so each service listens to its respective topic for incoming messages and produces messages to the next service's topic. The topics are as follows:

- `dough-machine`
- `dough-machine-done`
- `dough-shaper`
- `dough-shaper-done`
- `sauce-machine`
- `sauce-machine-done`
- `cheese-machine`
- `cheese-machine-done`
- `meat-machine`
- `meat-machine-done`
- `vegetables-machine`
- `vegetables-machine-done`
- `freezer-machine`
- `freezer-machine-done`
- `oven-machine`
- `oven-machine-done`
- `packaging-machine`
- `packaging-machine-done`

### Order Tracking Topics

Additionally, the following Kafka topics are used to track complete orders and measure their latency:

- `order-stack` – Stores incoming orders from customers in a queue. When the Order Processing service receives multiple orders, it stores them in this topic to be processed sequentially. Once the production line completes the current order, it proceeds to the next order in the stack. Future enhancements will include priority-based order selection.
- `order-processing` – The Order Processing service sends a message to this topic when it begins processing an order, including the order ID and start timestamp.
- `order-done` – The Packaging service sends a message to this topic when the order is complete, including the order ID and end timestamp.

## Kafka Messages

There are two main message types used in the production line:

### Pizza Order Message

- `pizzaId` (int): Unique pizza identifier.
- `orderId` (int): Unique order identifier.
- `orderSize` (int): Total number of pizzas in the order.
- `startTimestamp` (long): Timestamp when order processing started.
- `endTimestamp` (long): Timestamp when order processing ended.
- `msgDesc` (string): Description of the current processing step.
- `sauce` (string): Sauce type (e.g., `"tomato"`, `"pesto"`).
- `baked` (boolean): Whether the pizza has been baked.
- `cheese` (array of strings): List of cheese toppings.
- `meat` (array of strings): List of meat toppings.
- `veggies` (array of strings): List of vegetable toppings.

Example message:
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

### Pizza Done Message

Sent by a machine (e.g., `dough-machine-done`) to signal it is ready for the next pizza.
```json
{
  "pizzaId": 42,
  "orderId": 123,
  "doneMsg": true
}
```

### Order Processing Message

Sent to the `order-processing` topic when an order begins processing:
```json
{
  "orderId": 123,
  "startTimestamp": 1731571200000
}
```

### Order Done Message

Sent to the `order-done` topic when an order is completed:
```json
{
  "orderId": 123,
  "endTimestamp": 1731571380000
}
```

### Order Stack Message

(Future use) Stored in the `order-stack` topic for pending orders:
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