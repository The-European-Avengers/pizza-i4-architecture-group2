from py_helpers.kafka import KafkaClient
from internal_goods_provider.callbacks import CallbackHandler
from py_helpers.logger import get_logger
import logging
import signal
import sys

logger = get_logger("internal-goods-provider", level=logging.INFO, json_format=False)

consume_topics = ['dough-machine-restock', 'sauce-machine-restock', 'cheese-machine-restock', 'meat-machine-restock',
                  'vegetables-machine-restock', 'packaging-robot-restock']
kafka_client = KafkaClient(consume_topics)

def shutdown_handler(sig, frame):
    print('Shutting down...')
    kafka_client.stop()
    sys.exit(0)

signal.signal(signal.SIGINT, shutdown_handler)
callback_handler = CallbackHandler(kafka_client)

kafka_client.on_message(consume_topics[0], callback_handler.on_dough_machine)
kafka_client.on_message(consume_topics[1], callback_handler.on_sauce_machine)
kafka_client.on_message(consume_topics[2], callback_handler.on_cheese_machine)
kafka_client.on_message(consume_topics[3], callback_handler.on_meat_machine)
kafka_client.on_message(consume_topics[4], callback_handler.on_vegetables_machine)
kafka_client.on_message(consume_topics[5], callback_handler.on_packaging_robot)

kafka_client.start()
logger.info('Internal Goods Provider is running')

try:
    while True:
        pass
except KeyboardInterrupt:
    kafka_client.stop()