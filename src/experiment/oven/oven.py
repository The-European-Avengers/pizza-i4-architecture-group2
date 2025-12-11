import asyncio
import json
import signal
import uuid
from kafka import KafkaConsumer, KafkaProducer

running = True
next_machine_busy = False

def shutdown_handler(sig, frame):
    global running
    print("\n🛑 Stopping service...")
    running = False
    consumer.close()       # unblock main poll
    consumer_done.close()  # unblock monitor poll
    producer.close()

signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

# Kafka topics
consume_topic = "oven-machine"
produce_topic_next = "packaging-machine"
produce_topic_done = "oven-machine-done"
consume_topic_done = "packaging-machine-done"

KAFKA_BROKER = "kafka-experiment:29092"

# Producer WITH KEY SERIALIZER
producer = KafkaProducer(
    bootstrap_servers=KAFKA_BROKER,
    key_serializer=lambda k: k.encode("utf-8") if isinstance(k, str) else k,
    value_serializer=lambda v: json.dumps(v).encode("utf-8")
)

group_id = "oven-group"

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


async def process_pizza(pizza):
    global next_machine_busy

    pizza_id = pizza["pizzaId"]
    order_id = pizza["orderId"]
    composite_key = f"{pizza_id}_{order_id}"

    print(f"🔥 Starting oven for pizza {pizza_id}...")

    await asyncio.sleep(1)

    print(f"🔥 oven finished for pizza {pizza_id}")

    pizza["msgDesc"] = f"Pizza frozen with id {pizza_id} in order {order_id}"

    # Notify previous machine (Pizza Done Message)
    done_message = {
        "pizzaId": pizza_id,
        "orderId": order_id,
        "doneMsg": True
    }

    producer.send(produce_topic_done, key=composite_key, value=done_message)
    producer.flush()
    print(f"📤 Sent done event → {produce_topic_done}")

    # Wait for next machine
    while next_machine_busy:
        print("⏳ Next machine busy, waiting...")
        await asyncio.sleep(1)

    # Send updated Pizza Order Message to next machine
    producer.send(produce_topic_next, key=composite_key, value=pizza)
    producer.flush()
    next_machine_busy = True

    print(f"📤 Sent pizza {pizza_id} to next machine → {produce_topic_next}")


async def monitor_machine_done():
    global next_machine_busy

    print("🎧 Listening for next machine done messages...")

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
                    print(f"✅ Packaging machine free (pizzaId={data.get('pizzaId')})")


async def main_loop():
    print("oven machine ready\n")

    asyncio.create_task(monitor_machine_done())

    while running:
        msg_pack = consumer.poll(timeout_ms=500)

        if not msg_pack:
            await asyncio.sleep(0.1)
            continue

        for topic_partition, messages in msg_pack.items():
            for message in messages:
                pizza = message.value
                print(f"📥 Received pizza: {pizza}")

                await process_pizza(pizza)

    print("🛑 Stopped listening.")


try:
    asyncio.run(main_loop())
finally:
    consumer.close()
    producer.close()
    print("✔ Clean shutdown complete.")
