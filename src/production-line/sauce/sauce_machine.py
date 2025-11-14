import asyncio
import json
import time
import signal
from kafka import KafkaConsumer, KafkaProducer
from concurrent.futures import ThreadPoolExecutor
import uuid

running = True

def shutdown_handler(sig, frame):
    global running
    print("\n🛑 Stopping service...")
    running = False

signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

# Kafka topics
consume_topic = "sauce-machine"
produce_topic = "meat-machine"

KAFKA_BROKER = "kafka:29092"

# Initialize Kafka producer
producer = KafkaProducer(
    bootstrap_servers=KAFKA_BROKER,
    value_serializer=lambda v: json.dumps(v).encode('utf-8')
)

group_id = f"sauce-worker-{uuid.uuid4()}"  # makes a fresh group each run
# Initialize Kafka consumer
consumer = KafkaConsumer(
    consume_topic,
    bootstrap_servers=KAFKA_BROKER,
    auto_offset_reset='latest',
    enable_auto_commit=True,
    group_id=group_id,
    value_deserializer=lambda v: json.loads(v.decode('utf-8'))
)

# ThreadPool for simulating blocking work
executor = ThreadPoolExecutor(max_workers=5)

async def process_message(data):
    """Simulate work for a message and produce a result."""
    print(f"📥 Received: {data}")
    # Simulate blocking work in a thread
    print(f"🥫 Preparing sauce for order {data.get('id')}...")
    await asyncio.get_event_loop().run_in_executor(executor, time.sleep, 1)
    print(f"🥫 Sauce added to pizza for order {data.get('id')}.")
    # Produce result message
    result = {
        "id": data.get("id"),
        "timestamp": time.time(),
        "sauce": data.get("sauce"),
        "baked": data.get("baked"),
        "cheese": data.get("cheese"),
        "meat": data.get("meat"),
        "veggies": data.get("veggies")
    }
    producer.send(produce_topic, result)
    print(f"✅ Produced finished message: {result}")

async def consume_messages():
    """Consume messages asynchronously."""
    while running:
        raw_messages = consumer.poll(timeout_ms=1000)  # non-blocking poll
        for tp, messages in raw_messages.items():
            for message in messages:
                asyncio.create_task(process_message(message.value))
        await asyncio.sleep(0.1)

async def main():
    print(f"🎧 Listening on '{consume_topic}' and producing to '{produce_topic}'...\n")
    await consume_messages()

try:
    asyncio.run(main())
except KeyboardInterrupt:
    pass
finally:
    producer.flush()
    producer.close()
    consumer.close()
    executor.shutdown(wait=True)
    print("✅ Service stopped cleanly.")
