from typing import Union, Callable, Dict, Any
from kafka import KafkaConsumer, KafkaProducer
import json
import uuid
import threading
import queue


def get_consumer(topics: Union[list, str]) -> KafkaConsumer:
    """Helper function for getting Kafka Consumer object"""
    group_id = f"test-group-{uuid.uuid4()}"
    consumer = KafkaConsumer(
        bootstrap_servers='kafka-experiment:29092',
        auto_offset_reset='latest',
        enable_auto_commit=True,
        group_id=group_id,
        value_deserializer=lambda v: json.loads(v.decode('utf-8'))
    )
    consumer.subscribe(topics)
    return consumer


def get_producer():
    """Helper function for getting Kafka Producer object"""
    # Add key_serializer so that string keys are encoded as UTF-8 bytes
    return KafkaProducer(
        bootstrap_servers='kafka-experiment:29092',
        value_serializer=lambda v: json.dumps(v).encode('utf-8'),
        key_serializer=lambda k: k.encode('utf-8') if isinstance(k, str) else k
    )


class KafkaClient:
    """
    Unified Kafka client for both producing and consuming messages with callback support.
    This wraps KafkaProducer and KafkaConsumer for a simplified API.
    """

    def __init__(
            self,
            topics: Union[list, str],
            bootstrap_servers: str = 'kafka-experiment:29092',
            group_id: str = None
    ):
        """
        Initialize the Kafka client.

        Args:
            topics: Topic(s) to subscribe to (string or list of strings)
            bootstrap_servers: Kafka broker address
            group_id: Consumer group ID (auto-generated if not provided)
        """
        self.bootstrap_servers = bootstrap_servers
        self.group_id = group_id or f"group-{uuid.uuid4()}"
        self.producer = get_producer()
        self.consumer = get_consumer(topics)

        # Callback management
        self.callbacks: Dict[str, Callable] = {}
        self._running = False
        self._thread = None
        self._message_queue = queue.Queue()
        self._worker_thread = None

    def send(self, topic: str, message: Dict[str, Any], key: str = None, wait: bool = False):
        """
        Send a message to a specific topic.

        Args:
            topic: Target topic name
            message: Message payload (dict)
            wait: Whether to wait for confirmation (default: False)

        Returns:
            Future object representing the send operation
        """
        # Use explicit value= and key= so serialization is applied correctly
        future = self.producer.send(topic, key=key, value=message)
        if wait:
            return future.get(timeout=10)
        return future

    def on_message(self, topic: str, callback: Callable):
        """
        Register a callback function for a specific topic.

        Args:
            topic: Topic name to listen to
            callback: Function to call when message received (receives message object)
        """
        self.callbacks[topic] = callback

    def start(self):
        """Start consuming messages in a background thread."""
        if self._running:
            print("Consumer already running")
            return

        self._running = True
        self._thread = threading.Thread(target=self._consume_loop, daemon=True)
        self._thread.start()

        self._worker_thread = threading.Thread(target=self._process_queue_loop, daemon=True)
        self._worker_thread.start()

    def _consume_loop(self):
        """Internal loop that processes messages and triggers callbacks."""
        while self._running:
            try:
                for message in self.consumer:
                    if not self._running:
                        break

                    self._message_queue.put(message)

            except Exception as e:
                if self._running:
                    print(f"❌ Error in consume loop: {e}")

    def _process_queue_loop(self):
        """Worker thread pulls from queue and calls callbacks."""
        while self._running or not self._message_queue.empty():
            try:
                message = self._message_queue.get(timeout=1)
            except queue.Empty:
                continue

            topic = message.topic
            if topic in self.callbacks:
                try:
                    self.callbacks[topic](message)
                except Exception as e:
                    print(f"Error in callback for topic '{topic}': {e}")
            self._message_queue.task_done()

    def stop(self):
        """Stop consuming messages and close all connections."""
        print("🛑 Stopping Kafka client...")
        self._running = False

        if self._thread and self._thread.is_alive():
            self._thread.join(timeout=5)

        if self._worker_thread and self._worker_thread.is_alive():
            self._worker_thread.join(timeout=5)

        self.consumer.close()
        self.producer.flush()
        self.producer.close()
        print("✅ Kafka client stopped")

    def __enter__(self):
        """Context manager support."""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager cleanup."""
