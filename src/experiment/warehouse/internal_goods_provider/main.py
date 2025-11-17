from py_helpers.kafka import KafkaClient
from internal_goods_provider.callbacks import CallbackHandler
import signal
import sys

consume_topics = ['supply-events.request', 'goods-events.pickup', 'order-done']
kafka_client = KafkaClient(consume_topics)

def shutdown_handler(sig, frame):
    print('Shutting down...')
    kafka_client.stop()
    sys.exit(0)

signal.signal(signal.SIGINT, shutdown_handler)
callback_handler = CallbackHandler(kafka_client)

kafka_client.on_message(consume_topics[0], callback_handler.on_supply_event)
kafka_client.on_message(consume_topics[1], callback_handler.on_goods_events)
kafka_client.on_message(consume_topics[2], callback_handler.on_order_done)

kafka_client.start()
print('Internal Goods Provider is running')

try:
    while True:
        pass
except KeyboardInterrupt:
    kafka_client.stop()