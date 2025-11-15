import json
import random
import signal
import time

from kafka import KafkaProducer

running = True


# num_pizzas = int(input("Choose the number of pizzas to order: "))
num_pizzas = 1
print(f"You have ordered {num_pizzas} pizzas.")



def shutdown_handler(sig, frame):
    global running
    print("\n🛑 Stopping producer...")
    running = False

# Catch Ctrl+C
signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

KAFKA_BROKER = "kafka:9092"

# Initialize Kafka producer
producer = KafkaProducer(
    bootstrap_servers=KAFKA_BROKER,
    value_serializer=lambda v: json.dumps(v).encode('utf-8')
)

topic = "sauce-machine"
try:
    time.sleep(20)
    message = {
        "pizzaId": 1,
        "orderId": 12,
        "orderSize": 2,
        "startTimestamp": time.time(),
        "endTimestamp": None,
        "msgDesc": "Sauce added to pizza",
        "sauce": "tomato",
        "baked": True,
        "cheese": ["mozzarella"],
        "meat": ["pepperoni", "sausage"],
        "veggies": ["mushroom", "onion"]
    }
    producer.send(topic, message)
    print(f"✅ Sent: {message} to topic '{topic}'")
    message["pizzaId"] += 1
    time.sleep(0.5)
    producer.send(topic, message)
    producer.flush()
    print(f"✅ Sent: {message} to topic '{topic}'")



except Exception as e:
    print(f"❌ Error: {e}")

finally:
    producer.close()
    print("✅ Producer closed cleanly.")

