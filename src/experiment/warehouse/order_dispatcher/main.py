from py_helpers.kafka import KafkaClient
from order_dispatcher.callbacks import CallbackHandler
import signal
import sys

consume_topics = ['order-done']
kafka_client = KafkaClient(consume_topics)

def shutdown_handler(sig, frame):
    print('Shutting down...')
    kafka_client.stop()
    sys.exit(0)

signal.signal(signal.SIGINT, shutdown_handler)
callback_handler = CallbackHandler(kafka_client)

kafka_client.on_message(consume_topics[0], callback_handler.on_order_done)

kafka_client.start()
print('Internal Goods Provider is running')

try:
    while True:
        pass
except KeyboardInterrupt:
    kafka_client.stop()