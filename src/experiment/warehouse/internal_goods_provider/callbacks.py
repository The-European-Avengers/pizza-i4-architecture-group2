from py_helpers.kafka import KafkaClient
from datetime import datetime
from time import sleep

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client

    def on_dough_machine(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(1)
        self.client.send('dough-machine-restock-done', self.create_restock_msg(message))

    def on_sauce_machine(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(1)
        self.client.send('sauce-machine-restock-done', self.create_restock_msg(message))

    def on_cheese_machine(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(1)
        self.client.send('cheese-machine-restock-done', self.create_restock_msg(message))

    def on_meat_machine(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(1)
        self.client.send('meat-machine-restock-done', self.create_restock_msg(message))

    def on_vegetables_machine(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(1)
        self.client.send('vegetables-machine-restock-done', self.create_restock_msg(message))

    def on_packaging_robot(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(1)
        self.client.send('packaging-robot-restock-done', self.create_restock_msg(message))

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
            'completedTimestamp': datetime.now().replace(microsecond=0)
        }
