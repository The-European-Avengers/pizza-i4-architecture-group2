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
- `oven-machine`
- `oven-machine-done`
- `packaging-machine`
- `packaging-machine-done`

## Kafka Messages

There are two types of messages used in the production line:

### Pizza Order Message

- `id` (number): Unique order identifier.
- `msgDesc` (string): Description of the message.
- `sauce` (string): Sauce type (e.g., `"tomato"`, `"pesto"`).
- `baked` (boolean): Whether the pizza has been baked.
- `cheese` (array of strings): List of cheese toppings.
- `meat` (array of strings): List of meat toppings.
- `veggies` (array of strings): List of vegetable toppings.

Example message:

```json
{
  "id": 123,
  "msgDesc": "Sauce added to pizza with ID 123",
  "sauce": "tomato",
  "baked": false,
  "cheese": "mozzarella",
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
  "id": 123,
  "doneMsg": true
}
```

## Running the Experiment

To run the experiment, you need to run the `docker-compose.yml` file located in the `src` folder. This will start all the services and Kafka broker needed for the production line to function.

```bash
cd src
docker-compose up --build
```

## Getting the experiment results
