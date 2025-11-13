
def on_supply_event(message):
    print(f'Topic: {message.topic} Msg: {message.value}')

def on_goods_events(message):
    print(f'Topic: {message.topic} Msg: {message.value}')