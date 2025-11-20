from kafka import KafkaConsumer
import json
import uuid

group_id = f"test-group-{uuid.uuid4()}"
# Initialize Kafka consumer
consumer = KafkaConsumer(
    'order-done',
    bootstrap_servers='kafka:9092',
    auto_offset_reset='latest',   # start from latest message
    enable_auto_commit=True,
    group_id=group_id,
    value_deserializer=lambda v: json.loads(v.decode('utf-8'))
)

print("🔍 Listening for messages... (Ctrl+C to stop)")

for message in consumer:
    print(f"📥 Received: {message.value}")
