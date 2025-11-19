from py_helpers.kafka import KafkaClient
from datetime import datetime
from time import sleep

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client
        print("✅ Order Dispatcher CallbackHandler initialized")

    def on_order_done(self, message):
        print('=' * 80)
        print(f'🎉 ORDER DONE MESSAGE RECEIVED!')
        print(f'   Topic: {message.topic}')
        print(f'   Message: {message.value}')
        print('=' * 80)
        
        sleep(5)  # Simulate dispatch processing time
        
        dispatch_message = {
            'orderId': message.value['orderId'],
            'msgDesc': 'Order dispatched successfully',
            'dispatchedTimestamp': int(datetime.now().timestamp() * 1000)  # Milliseconds
        }
        
        print(f'📤 DISPATCHING ORDER TO: order-dispatch')
        print(f'   Dispatch message: {dispatch_message}')
        
        self.client.send('order-dispatched', dispatch_message)
        # ✅ CRITICAL FIX: Flush the producer to ensure message is sent immediately
        self.client.producer.flush()
        
        print(f'✅ ORDER DISPATCHED SUCCESSFULLY')
        print('=' * 80)