from py_helpers.kafka import KafkaClient
from py_helpers.logger import get_logger
import logging
from datetime import datetime
from time import sleep

logger = get_logger("order-dispatcher", level=logging.INFO, json_format=False)

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client

    def on_order_done(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(5)  # Simulate dispatch processing time
        dispatch_message = {
            'orderId': message.value['orderId'],
            'msgDesc': 'Order dispatched successfully',
            'dispatchedTimestamp': int(datetime.now().timestamp() * 1000)  # Milliseconds
        }
        self.client.send('order-dispatched', dispatch_message)
        # ✅ CRITICAL FIX: Flush the producer to ensure message is sent immediately
        self.client.producer.flush()
        logger.info('ORDER DISPATCHED SUCCESSFULLY')
