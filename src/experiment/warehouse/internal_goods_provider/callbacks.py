from py_helpers.kafka import KafkaClient
from datetime import datetime
from time import sleep

class CallbackHandler:

    def __init__(self, client: KafkaClient):
        super().__init__()
        self.client = client
        print("✅ CallbackHandler initialized")

    def on_dough_machine(self, message):
        print('=' * 80)
        print(f'🔔 DOUGH RESTOCK REQUEST RECEIVED!')
        print(f'   Topic: {message.topic}')
        print(f'   Message: {message.value}')
        print('=' * 80)
        
        sleep(1)  # Simulate processing time
        
        response = self.create_restock_msg(message)
        print(f'📤 SENDING RESTOCK RESPONSE TO: dough-machine-restock-done')
        print(f'   Response: {response}')
        
        self.client.send('dough-machine-restock-done', response)
        # ✅ CRITICAL FIX: Flush the producer to ensure message is sent immediately
        self.client.producer.flush()
        print(f'✅ DOUGH RESTOCK RESPONSE SENT AND FLUSHED')
        print('=' * 80)

    def on_sauce_machine(self, message):
        print(f'🔔 SAUCE RESTOCK REQUEST: {message.topic} - {message.value}')
        sleep(1)
        self.client.send('sauce-machine-restock-done', self.create_restock_msg(message))
        self.client.producer.flush()  # ✅ FLUSH
        print(f'✅ SAUCE RESTOCK COMPLETED')

    def on_cheese_machine(self, message):
        print(f'🔔 CHEESE RESTOCK REQUEST: {message.topic} - {message.value}')
        sleep(1)
        self.client.send('cheese-machine-restock-done', self.create_restock_msg(message))
        self.client.producer.flush()  # ✅ FLUSH
        print(f'✅ CHEESE RESTOCK COMPLETED')

    def on_meat_machine(self, message):
        print(f'🔔 MEAT RESTOCK REQUEST: {message.topic} - {message.value}')
        sleep(1)
        self.client.send('meat-machine-restock-done', self.create_restock_msg(message))
        self.client.producer.flush()  # ✅ FLUSH
        print(f'✅ MEAT RESTOCK COMPLETED')

    def on_vegetables_machine(self, message):
        print(f'🔔 VEGETABLES RESTOCK REQUEST: {message.topic} - {message.value}')
        sleep(1)
        self.client.send('vegetables-machine-restock-done', self.create_restock_msg(message))
        self.client.producer.flush()  # ✅ FLUSH
        print(f'✅ VEGETABLES RESTOCK COMPLETED')

    def on_packaging_robot(self, message):
        print(f'🔔 PACKAGING RESTOCK REQUEST: {message.topic} - {message.value}')
        sleep(1)
        self.client.send('packaging-robot-restock-done', self.create_restock_msg(message))
        self.client.producer.flush()  # ✅ FLUSH
        print(f'✅ PACKAGING RESTOCK COMPLETED')

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