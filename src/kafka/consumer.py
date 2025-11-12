from kafka import KafkaConsumer
import json

# Initialize Kafka consumer
consumer = KafkaConsumer(
    'test-topic',
    bootstrap_servers='192.168.1.178:9092',
    auto_offset_reset='earliest',   # start from earliest message
    enable_auto_commit=True,
    group_id='test-group',
    value_deserializer=lambda v: json.loads(v.decode('utf-8'))
)

print("🔍 Listening for messages... (Ctrl+C to stop)")

for message in consumer:
    print(f"📥 Received: {message.value}")
