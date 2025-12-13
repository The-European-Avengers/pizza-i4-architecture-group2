from py_helpers.kafka import KafkaClient
from py_helpers.logger import get_logger
import logging
from datetime import datetime
from time import sleep

logger = get_logger("internal-goods-provider", level=logging.INFO, json_format=False)

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client

    def on_dough_machine(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(35)  # Simulate processing time
        # preserve the original message key if present; fall back to machineId from value
        key = message.key
        if isinstance(key, (bytes, bytearray)):
            try:
                key = key.decode('utf-8')
            except Exception:
                pass

        if key is None:
            key = message.value.get('machineId')
        self.client.send('dough-machine-restock-done', self.create_restock_msg(message), key=key)
        self.client.producer.flush()
        logger.info('DOUGH RESTOCK COMPLETED')

    def on_sauce_machine(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(35)
        # preserve the original message key if present; fall back to machineId from value
        key = message.key
        if isinstance(key, (bytes, bytearray)):
            try:
                key = key.decode('utf-8')
            except Exception:
                pass

        if key is None:
            key = message.value.get('machineId')

        self.client.send('sauce-machine-restock-done', self.create_restock_msg(message), key=key)
        self.client.producer.flush()  # ✅ FLUSH
        logger.info('SAUCE RESTOCK COMPLETED')

    def on_cheese_machine(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(35)
        # preserve the original message key if present; fall back to machineId from value
        key = message.key
        if isinstance(key, (bytes, bytearray)):
            try:
                key = key.decode('utf-8')
            except Exception:
                pass

        if key is None:
            key = message.value.get('machineId')
        self.client.send('cheese-machine-restock-done', self.create_restock_msg(message), key=key)
        self.client.producer.flush()  # FLUSH
        logger.info('CHEESE RESTOCK COMPLETED')

    def on_meat_machine(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(35)
        # preserve the original message key if present; fall back to machineId from value
        key = message.key
        if isinstance(key, (bytes, bytearray)):
            try:
                key = key.decode('utf-8')
            except Exception:
                pass

        if key is None:
            key = message.value.get('machineId')
        self.client.send('meat-machine-restock-done', self.create_restock_msg(message), key=key)
        self.client.producer.flush()  # FLUSH
        logger.info('MEAT RESTOCK COMPLETED')

    def on_vegetables_machine(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(35)
        # preserve the original message key if present; fall back to machineId from value
        key = message.key
        if isinstance(key, (bytes, bytearray)):
            try:
                key = key.decode('utf-8')
            except Exception:
                pass

        if key is None:
            key = message.value.get('machineId')
        self.client.send('vegetables-machine-restock-done', self.create_restock_msg(message), key=key)
        self.client.producer.flush()  # FLUSH
        logger.info('VEGETABLES RESTOCK COMPLETED')

    def on_packaging_robot(self, message):
        logger.info(f'Topic: {message.topic} Message: {message.value}')
        sleep(35)
        # preserve the original message key if present; fall back to machineId from value
        key = message.key
        if isinstance(key, (bytes, bytearray)):
            try:
                key = key.decode('utf-8')
            except Exception:
                pass

        if key is None:
            key = message.value.get('machineId')
        self.client.send('packaging-machine-restock-done', self.create_restock_msg(message), key=key)
        self.client.producer.flush()  # FLUSH
        logger.info('PACKAGING RESTOCK COMPLETED')

    @staticmethod
    def create_restock_msg(message):
        items = list()
        for item in message.value['items']:
            items.append({
                'itemType': item['itemType'],
                'deliveredAmount': item['requestedAmount'],
            })

        return {
            'machineId': message.value['machineId'],
            'items': items,
            'completedTimestamp': int(datetime.now().timestamp() * 1000)  # Milliseconds
        }