from src.py_helpers.kafka import KafkaClient
import callbacks
import signal
import sys

consume_topics = ['supply-events.request', 'goods-events.pickup']
kafka_client = KafkaClient(consume_topics)

def shutdown_handler(sig, frame):
    print('Shutting down...')
    kafka_client.stop()
    sys.exit(0)

signal.signal(signal.SIGINT, shutdown_handler)

kafka_client.on_message(consume_topics[0], callbacks.on_supply_event)
kafka_client.on_message(consume_topics[1], callbacks.on_goods_events)

kafka_client.start()
print('Internal Goods Provider is running')

try:
    while True:
        pass
except KeyboardInterrupt:
    kafka_client.stop()