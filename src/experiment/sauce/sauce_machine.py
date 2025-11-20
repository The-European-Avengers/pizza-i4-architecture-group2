import asyncio
import json
import signal
import time
from kafka import KafkaConsumer, KafkaProducer

running = True
next_machine_busy = False

MAX_STOCK = 100

# Sauce stock levels
sauce_stock = {
    "tomato": 11,
    "BBQ Sauce": 11,
    "Pesto": 11,
    "Olive Oil": 11,
    "Sriracha-Tomato Blend": 11,
    "White Garlic Cream": 11,
    "Hollandaise Sauce": 11
}

def shutdown_handler(sig, frame):
    global running
    print("\nStopping service...")
    running = False
    consumer.close()
    consumer_done.close()
    consumer_restock_done.close()
    producer.close()

signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

# Kafka topics
consume_topic = "sauce-machine"
produce_topic_next = "cheese-machine"
produce_topic_done = "sauce-machine-done"
consume_topic_done = "cheese-machine-done"

# Restocking topics
restock_request_topic = "sauce-machine-restock"
restock_done_topic = "sauce-machine-restock-done"

KAFKA_BROKER = "kafka-experiment:29092"

# Producer
producer = KafkaProducer(
    bootstrap_servers=KAFKA_BROKER,
    value_serializer=lambda v: json.dumps(v).encode("utf-8")
)

group_id = "sauce-machine-group"

# Pizza consumer
consumer = KafkaConsumer(
    consume_topic,
    bootstrap_servers=KAFKA_BROKER,
    auto_offset_reset="latest",
    enable_auto_commit=True,
    group_id=group_id,
    value_deserializer=lambda v: json.loads(v.decode("utf-8"))
)

# Done consumer
consumer_done = KafkaConsumer(
    consume_topic_done,
    bootstrap_servers=KAFKA_BROKER,
    auto_offset_reset="latest",
    enable_auto_commit=True,
    group_id=group_id + "-done",
    value_deserializer=lambda v: json.loads(v.decode("utf-8"))
)

# Restock-done consumer
consumer_restock_done = KafkaConsumer(
    restock_done_topic,
    bootstrap_servers=KAFKA_BROKER,
    auto_offset_reset="latest",
    enable_auto_commit=True,
    group_id=group_id + "-restock-done",
    value_deserializer=lambda v: json.loads(v.decode("utf-8"))
)

# Tracks pending restock requests (avoid spamming requests)
restock_in_progress = False


def build_restock_request():
    """Creates the restock request message according to the specification."""
    items = []

    for sauce, stock in sauce_stock.items():
        if stock <= 20:  # Include all low-stock sauces
            requested = MAX_STOCK - stock
            items.append({
                "itemType": sauce,
                "currentStock": stock,
                "requestedAmount": requested
            })

    msg = {
        "machineId": "sauce-machine",
        "items": items,
        "requestTimestamp": int(time.time() * 1000)
    }

    return msg


def check_restock_needed():
    """Returns True if any sauce is <=10% of max stock."""
    return any(stock <= 10 for stock in sauce_stock.values())


def trigger_restock_request():
    global restock_in_progress

    request = build_restock_request()

    if not request["items"]:
        return  # nothing to restock

    print("Restock request sent:", request)

    producer.send(restock_request_topic, request)
    producer.flush()

    restock_in_progress = True


async def monitor_restock_done():
    """Handle restock completion events."""
    global restock_in_progress

    print("Listening for restock completion messages...")

    while running:
        msg_pack = consumer_restock_done.poll(timeout_ms=500)

        if not msg_pack:
            await asyncio.sleep(0.1)
            continue

        for _, messages in msg_pack.items():
            for message in messages:
                data = message.value
                items = data.get("items", [])

                for item in items:
                    sauce = item["itemType"]
                    delivered = item["deliveredAmount"]

                    if sauce in sauce_stock:
                        sauce_stock[sauce] += delivered
                        if sauce_stock[sauce] > MAX_STOCK:
                            sauce_stock[sauce] = MAX_STOCK

                        print(
                            f"Restock done for {sauce}: +{delivered}, stock now {sauce_stock[sauce]}"
                        )

                restock_in_progress = False


async def wait_for_sauce(sauce_type):
    """Wait until sauce is available."""
    while sauce_stock.get(sauce_type, 0) <= 0:
        print(f"Sauce '{sauce_type}' out of stock. Waiting...")
        await asyncio.sleep(1)


async def process_pizza(pizza):
    global next_machine_busy, sauce_stock, restock_in_progress

    pizza_id = pizza["pizzaId"]
    sauce_type = pizza.get("sauce")

    if sauce_type not in sauce_stock:
        print(f"Error: unknown sauce '{sauce_type}'")
        return

    print(f"Preparing sauce for pizza {pizza_id} using {sauce_type}...")

    # Wait if unavailable
    await wait_for_sauce(sauce_type)

    # Simulated work
    await asyncio.sleep(1)
    print(f"🥫 Sauce '{sauce_type}' added to pizza {pizza_id}")

    # Decrease stock
    sauce_stock[sauce_type] -= 1
    print(f"🥫 Stock for '{sauce_type}' is now {sauce_stock[sauce_type]}")

    # Restock logic
    if not restock_in_progress and check_restock_needed():
        trigger_restock_request()

    # Update message
    pizza["msgDesc"] = (
        f"Sauce '{sauce_type}' added to pizza {pizza_id} "
        f"in order {pizza['orderId']}"
    )

    # Send done to previous machine
    done_msg = {
        "pizzaId": pizza["pizzaId"],
        "orderId": pizza["orderId"],
        "doneMsg": True
    }

    producer.send(produce_topic_done, done_msg)
    producer.flush()

    print(f"📤 Done event sent -> {produce_topic_done} for pizza {pizza_id}")

    # Wait for next machine
    while next_machine_busy:
        print("⏳ Next machine busy, waiting...")
        await asyncio.sleep(1)

    producer.send(produce_topic_next, pizza)
    producer.flush()
    next_machine_busy = True

    print(f"📤 Pizza {pizza_id} sent to next machine -> {produce_topic_next}")


async def monitor_machine_done():
    global next_machine_busy

    print("Listening for next machine done messages...")

    while running:
        msg_pack = consumer_done.poll(timeout_ms=500)

        if not msg_pack:
            await asyncio.sleep(0.1)
            continue

        for _, messages in msg_pack.items():
            for message in messages:
                data = message.value
                if data.get("doneMsg") == True:
                    next_machine_busy = False
                    print(f"Cheese machine free (pizzaId={data.get('pizzaId')})")


async def main_loop():
    print("Sauce machine ready\n")

    asyncio.create_task(monitor_machine_done())
    asyncio.create_task(monitor_restock_done())

    while running:
        msg_pack = consumer.poll(timeout_ms=500)

        if not msg_pack:
            await asyncio.sleep(0.1)
            continue

        for _, messages in msg_pack.items():
            for message in messages:
                pizza = message.value
                print(f"Received pizza: {pizza}")
                await process_pizza(pizza)

    print("Stopped listening.")


try:
    asyncio.run(main_loop())
finally:
    consumer.close()
    producer.close()
    consumer_done.close()
    consumer_restock_done.close()
    print("Clean shutdown complete.")
