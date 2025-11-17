from py_helpers.kafka import KafkaClient
from datetime import datetime

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client

    def on_supply_event(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')

    def on_goods_events(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        self.client.send('goods-events.dispatch', {
            'order_id': 'random-uuid'
        })

    def on_order_done(self, message):
        print(f'Topic: {message.topic} Msg: {message.value}')
        self.client.send('order-dispatch', {
            'order_id': message.value['orderId'],
            'endTimestamp': datetime.now().replace(microsecond=0)
        })
