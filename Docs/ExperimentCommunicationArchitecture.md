# Experiment Communication Architecture

This diagram illustrates the event-driven Kafka-based communication used in the performance experiment (Section VII).

![Experiment Communication Diagram: Kafka-based event-driven pizza production pipeline showing three zones - External Interface Layer with Frontend and API Gateway on the left, central Kafka Cluster with production flow topics (dough-machine, sauce-machine, cheese-machine, etc.), completion signal topics (*-done), and restocking topics (*-restock), and Production Line Services on the right including Order Processing, eight sequential production machines (Dough Machine through Packaging Robot), and Internal Goods Provider. Arrows indicate message flow direction through Kafka topics, demonstrating the consumer-processor-producer pattern where each service consumes from input topics, processes pizzas, and publishes to output topics for the next stage.](diagram-images/experiment-communication.png)

## Architecture Overview

The architecture consists of three main zones:

### External Interface Layer
- **Frontend**: Submits orders
- **API Gateway**: Routes requests to internal services

### Kafka Cluster
Implements event-driven communication with topics for:
- Production flow: `dough-machine`, `sauce-machine`, `cheese-machine`, etc.
- Completion signals: `*-done` topics
- Restocking: `*-restock` and `*-restock-done` topics
- Order coordination: `order-stack`, `order-done`, `pizza-done`

### Production Line Services

**Order Coordination:**
- **Order Processing**: Manages pizza batches and production flow
- **Order Dispatcher**: Handles completion and pickup notifications

**Production Machines (Consumer-Processor-Producer pattern):**
Each machine consumes from input topic, processes pizza, and publishes to next stage:

1. Dough Machine → Dough Shaper → Sauce Machine → Cheese Grater → Meat Slicer → Vegetables Slicer
2. Vegetables Slicer routes to: Oven (baked) or Freezer (frozen)
3. Oven/Freezer → Packaging Robot → Order Dispatcher

**Supply Chain:**
- **Internal Goods Provider**: Manages ingredient distribution from warehouse to production machines
- Responds to restocking requests via machine-specific restock topics
- Sends completion confirmations via `*-restock-done` topics

## Communication Patterns

**Supply Chain:**
- **Internal Goods Provider**: Manages ingredient distribution and responds to restocking requests.)
3. Publishes updated pizza state to the next stage's topic

### Backpressure Mechanism
Machines signal completion via `*-done` topics, implementing backpressure:
- Order Processing waits for `dough-machine-done` before dispatching next pizza
### Forward Production Flow
Each machine consumes from input topic, processes pizza, and publishes to next stage's topic.
1. Machine publishes to its `*-restock` topic
2. Internal Goods Provider consumes request
3. Provider publishes confirmation to `*-restock-done` topic
4. Machine resumes production after receiving confirmation
### Backpressure Mechanism
Machines signal completion via `*-done` topics. Order Processing waits for signals before dispatching, preventing buffer overflow (performance tactic #3: Bound Queue Sizes).heese), Python (Sauce, Oven, Freezer), Go (Meat, Vegetables, Packaging)
- **API Layer**: Node.js/TypeScript (Nest.js framework)

## Measurement Points
Latency measurements are derived from timestamps embedded in Kafka messages:
### Restocking Communication
When stock ≤ 10 units: machine publishes restock request → Internal Goods Provider responds via `*-restock-done` → machine resumes production.
## Technology Stack
- **Message Broker**: Kafka (Redpanda)
- **Stream Processing**: ksqlDB
- **Services**: C# (Order Processing, Dough, Cheese), Python (Sauce, Oven, Freezer), Go (Meat, Vegetables, Packaging), Node.js/TypeScript (API)This event-driven architecture implements performance tactics #2 (stateless computation), #3 (bounded queues), and #4 (resource scheduling).