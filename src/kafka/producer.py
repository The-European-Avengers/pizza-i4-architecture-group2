import json
import random
import signal
import time

from kafka import KafkaProducer

running = True

def shutdown_handler(sig, frame):
    global running
    print("\n🛑 Stopping producer...")
    running = False

# Catch Ctrl+C
signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

# Initialize Kafka producer
producer = KafkaProducer(
    bootstrap_servers='127.0.0.1:9092',
    value_serializer=lambda v: json.dumps(v).encode('utf-8')
)

topic = "sauce-machine"
counter = 0

print(f"🚀 Producing messages to topic '{topic}' (Ctrl+C to stop)\n")

try:
    i = 0
    while i < 2 and running:
        message = {
            "id": counter,
            "timestamp": time.time(),
            "sauce": "tomato sauce",
            "baked": True,
            "cheese": "mozzarella",
            "meat": ["pepperoni", "bacon"],
            "veggies": ["onion", "mushroom"]
        }
        producer.send(topic, message)
        print(f"✅ Sent: {message} to topic '{topic}'")
        counter += 1
        i += 1

        # Random delay between 0.5 and 2 seconds
        time.sleep(5)

except Exception as e:
    print(f"❌ Error: {e}")

finally:
    producer.flush()
    producer.close()
    print("✅ Producer closed cleanly.")

