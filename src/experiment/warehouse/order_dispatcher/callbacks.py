from py_helpers.kafka import KafkaClient
from datetime import datetime
from time import sleep

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client

    def on_order_done(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        sleep(5)
        self.client.send('order-dispatched', {
            'orderId': message.value['orderId'],
            'orderSize': message.value['orderSize'],
            'msgDesc': 'Order dispatched successfully',
            'dispatchedTimestamp': datetime.now().replace(microsecond=0)
        })
