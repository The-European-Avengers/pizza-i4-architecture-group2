import json
import random
import signal
import time

from kafka import KafkaProducer

running = True


num_pizzas = int(input("Choose the number of pizzas to order: "))
print(f"You have ordered {num_pizzas} pizzas.")



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
try:
    message = {
        "id": 0,
        "order_number": num_pizzas,
        "timestamp": time.time(),
        "sauce": "tomato sauce",
        "baked": True,
        "cheese": "mozzarella",
        "meat": ["pepperoni", "bacon"],
        "veggies": ["onion", "mushroom"]
    }
    producer.send(topic, message)
    print(f"✅ Sent: {message} to topic '{topic}'")



except Exception as e:
    print(f"❌ Error: {e}")

finally:
    producer.flush()
    producer.close()
    print("✅ Producer closed cleanly.")

