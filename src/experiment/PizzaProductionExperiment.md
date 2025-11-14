# Production Line

## Folder Structure

Each service must be in its own folder with a `Dockerfile`.
The main production line services are:

- `dough-machine`: Handles dough preparation.
- `dough-shaper`: Shapes the dough.
- `sauce`: Prepares the sauce.
- `cheese`: Manages cheese processing.
- `meat`: Manages meat processing.
- `vegetables`: Takes care of vegetable processing.
- `freezer`: Manages freezing processes.
- `oven`: Handles baking.
- `packaging`: Manages packaging of the final product.

To simulate customer orders, use the `customer` folder which contains a simple script to create orders asking the number of pizzas desired.

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
- `id` (number): Unique order identifier.
- `doneMsg` (boolean): Indicates if the pizza is done.

Example message:

```json
{
  "pizzaId":42,
  "orderId":123,
  "doneMsg":true
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

Stored in the `order-stack` topic for pending orders:

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


## Running the Experiment

To run the experiment, you need to run the `docker-compose.yml` file located in the `src` folder. This will start all the services and Kafka broker needed for the production line to function.

```bash
cd src
docker-compose up --build
```

## Getting the experiment results
