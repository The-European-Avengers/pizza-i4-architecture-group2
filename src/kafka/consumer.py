from kafka import KafkaConsumer
import json
import uuid

group_id = f"test-group-{uuid.uuid4()}"
# Initialize Kafka consumer
consumer = KafkaConsumer(
    'test-topic',
    bootstrap_servers='127.0.0.1:9092',
    auto_offset_reset='earliest',   # start from earliest message
    enable_auto_commit=True,
    group_id=group_id,
    value_deserializer=lambda v: json.loads(v.decode('utf-8'))
)

print("🔍 Listening for messages... (Ctrl+C to stop)")

for message in consumer:
    print(f"📥 Received: {message.value}")
