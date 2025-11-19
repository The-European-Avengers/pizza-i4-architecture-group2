import asyncio
import json
import signal
import uuid
from kafka import KafkaConsumer, KafkaProducer

running = True
next_machine_busy = False

# Sauce stock levels (configure freely)
sauce_stock = {
    "tomato": 10,
    "BBQ Sauce": 8,
    "Pesto": 5,
    "Olive Oil": 12,
    "Sriracha-Tomato Blend": 6,
    "White Garlic Cream": 4,
    "Hollandaise Sauce": 3
}

def shutdown_handler(sig, frame):
    global running
    print("\nStopping sauce service 🥫🛑...")
    running = False
    consumer.close()
    consumer_done.close()
    producer.close()

signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

# Kafka topics
consume_topic = "sauce-machine"
produce_topic_next = "cheese-machine" 
produce_topic_done = "sauce-machine-done"
consume_topic_done = "cheese-machine-done"

KAFKA_BROKER = "kafka-experiment:29092"

# Producer
producer = KafkaProducer(
    bootstrap_servers=KAFKA_BROKER,
    value_serializer=lambda v: json.dumps(v).encode("utf-8")
)

group_id = "sauce-machine-group"

# Consumers
consumer = KafkaConsumer(
    consume_topic,
    bootstrap_servers=KAFKA_BROKER,
    auto_offset_reset="latest",
    enable_auto_commit=True,
    group_id=group_id,
    value_deserializer=lambda v: json.loads(v.decode("utf-8"))
)

consumer_done = KafkaConsumer(
    consume_topic_done,
    bootstrap_servers=KAFKA_BROKER,
    auto_offset_reset="latest",
    enable_auto_commit=True,
    group_id=group_id + "-done",
    value_deserializer=lambda v: json.loads(v.decode("utf-8"))
)


async def wait_for_sauce(sauce_type):
    """Wait until sauce stock becomes available."""
    while sauce_stock.get(sauce_type, 0) <= 0:
        print(f"Sauce '{sauce_type}' out of stock. Waiting for replenishment...")
        await asyncio.sleep(1)


async def process_pizza(pizza):
    global next_machine_busy, sauce_stock

    pizza_id = pizza["pizzaId"]
    sauce_type = pizza.get("sauceType")

    if sauce_type not in sauce_stock:
        print(f"Error: sauce type '{sauce_type}' not recognized.")
        return

    print(f"🥫Preparing sauce for pizza {pizza_id} using {sauce_type}...")

    # Wait if this sauce is not available
    await wait_for_sauce(sauce_type)

    # Simulated work
    await asyncio.sleep(1)
    print(f"🥫Sauce '{sauce_type}' added to pizza {pizza_id}")

    # Reduce sauce stock by 1
    sauce_stock[sauce_type] -= 1
    print(f"🥫Stock for '{sauce_type}' is now {sauce_stock[sauce_type]}")
    # Update message description
    pizza["msgDesc"] = (
        f"Sauce '{sauce_type}' added to pizza with id {pizza_id} "
        f"in order {pizza['orderId']}"
    )

    # Notify previous machine
    done_message = {
        "pizzaId": pizza["pizzaId"],
        "orderId": pizza["orderId"],
        "doneMsg": True
    }

    producer.send(produce_topic_done, done_message)
    producer.flush()
    print(f"📤 Sent done event -> {produce_topic_done}")

    # Wait for next machine
    while next_machine_busy:
        print("⏳ Next machine busy, waiting...")
        await asyncio.sleep(1)

    # Send pizza forward
    producer.send(produce_topic_next, pizza)
    producer.flush()
    next_machine_busy = True

    print(f"📤 Sent pizza {pizza_id} to next machine -> {produce_topic_next}")


async def monitor_machine_done():
    global next_machine_busy

    print("Listening for next machine done messages...")

    while running:
        msg_pack = consumer_done.poll(timeout_ms=500)

        if not msg_pack:
            await asyncio.sleep(0.1)
            continue

        for topic_partition, messages in msg_pack.items():
            for message in messages:
                data = message.value
                if data.get("doneMsg") == True:
                    next_machine_busy = False
                    print(
                        f"Cheese machine free (pizzaId={data.get('pizzaId')})"
                    )


async def main_loop():
    print("Sauce machine ready\n")

    asyncio.create_task(monitor_machine_done())

    while running:
        msg_pack = consumer.poll(timeout_ms=500)

        if not msg_pack:
            await asyncio.sleep(0.1)
            continue

        for topic_partition, messages in msg_pack.items():
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
    print("Clean shutdown complete.")
