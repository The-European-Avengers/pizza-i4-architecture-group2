from src.py_helpers.kafka import KafkaClient


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

