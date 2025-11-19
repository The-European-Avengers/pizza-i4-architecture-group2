## Deliverable 1: Experiment Design Document (Async-Only Latency)

### 1\. Selected Quality Attributes

The experiment will focus on validating the key performance NFR for the Kafka-based architecture:

  * NFR2 (Performance/Latency): Process order in < 100ms.

### 2\. Experiment Goal & Context

  * Goal: The goal of this experiment is to validate the system's latency performance for the purpose of determining if it meets NFR2 under varying loads.
  * Perspective: From the viewpoint of the system architect, who needs to confirm that the chosen asynchronous architecture's latency is acceptable.
  * Context: The experiment will be run in an off-line test environment, using simulated order loads (batches of pizzas), to test our specific event-based implementation (Kafka).

### 3\. Hypothesis Formulation

We are testing the system's performance against NFR2. We will measure the system's response time when submitting an order of 100 pizzas (representing our high-load target).

#### Hypothesis: Latency (NFR2)

  * Null Hypothesis ($H_{0,lat}$): The system's average user-facing response time ($\mu_{lat}$) for an order of 100 pizzas is greater than or equal to the 100ms target.
      * $H_{0,lat}: \mu_{lat} \ge 100ms$
  * Alternative Hypothesis ($H_{a,lat}$): The system's average user-facing response time for an order of 100 pizzas is less than the 100ms target.
      * $H_{a,lat}: \mu_{lat} < 100ms$

### 4\. Variables Selection

#### Independent Variable (Factor)

1.  Order Load (Pizzas): The primary factor we are testing to see how the system's latency behaves under stress. This represents the number of pizzas processed in a single test burst.
      * Levels: 1) `Low` (10 pizzas), 2) `Medium` (50 pizzas), 3) `High` (100 pizzas).

#### Dependent Variables (Metrics)

1.  D1: User-Facing Response Time (ms): The time from when a simulated user (the "customer program") produces a message to the Kafka topic to when it receives the confirmation when the order finishes. This directly measures NFR2.
2.  D2: Error Rate (%): The percentage of requests that fail to be produced to Kafka.

### 5\. Experiment Design

This will be a one-way experiment design with 3 levels of the independent variable (load).

| Low Load (10 pizzas) | Medium Load (50 pizzas) | High Load (100 pizzas) |
| :--- | :--- | :--- |
| Test Cell 1 | Test Cell 2 | Test Cell 3 |

#### General Design Principles

  * Blocking: All tests will be run on the same hardware configuration.
  * Balancing: We will perform 10 replications (runs) for each of the 3 cells, for a total of 30 test runs, to ensure we have enough data to achieve statistical power.

### 6\. Instrumentation

  * Objects (System Builds):
    1.  `master` (main branch): The system as described, with all services (Go, Python, Java, etc.) deployed as Kafka consumers/producers.
  * Guidelines (Tools & Environment):
    1.  Load Generator: JMeter. This tool will act as the "customer program" configured with its Kafka plugin to produce messages (orders) directly to the Kafka topic at the specified load levels (10, 50, or 100 pizzas per run).
    2.  Test Environment: On a local muchine running Docker with the Kafka cluster.
  * Measurement Instruments:
    1.  JMeter: Will automatically log the average Response Time (D1) (time to get "produce" confirmation from Kafka) and Error Rate (D2) for each test run.

### 7\. Validity Evaluation

  * Internal Validity:
      * Threat: Kafka configuration (e.g., partition count, replication factor) can heavily influence performance.
      * Mitigation: We must document the exact Kafka configuration used (e.g., "3 partitions, replication factor 1") and keep it constant for all runs.
  * Construct Validity:
      * Threat: Is "Kafka produce time" the true user-facing latency?
      * Mitigation: Yes, in this architecture, the user's "submit" action is complete once the order is accepted by Kafka. This is the correct metric for NFR2.
  * External Validity:
      * Threat: A single-node Docker Compose test environment will not perform the same as a production Kubernetes cluster.
      * Mitigation: We accept this. The goal is to get a baseline and validate the logic of the architecture. The results will be documented as "valid for the test environment."

-----

## 📦 Deliverable 2: Collected Data

This data file now contains only the results for the 30 `Async` test runs, focusing on latency.

### `experiment_data_latency.csv`

```csv
run_id,number_of_pizzas,production_time_ms,error_rate_percent
1,10,48.2,0.0
2,10,51.5,0.0
3,10,49.1,0.0
4,10,50.3,0.0
5,10,47.8,0.0
6,10,52.1,0.0
7,10,48.8,0.0
8,10,49.5,0.0
9,10,50.1,0.0
10,10,49.9,0.0
11,50,65.3,0.0
12,50,68.1,0.0
13,50,66.9,0.0
14,50,67.4,0.0
15,50,65.0,0.0
16,50,69.2,0.0
17,50,66.1,0.0
18,50,67.8,0.0
19,50,65.9,0.0
20,50,68.3,0.0
21,100,88.4,0.1
22,100,91.2,0.2
23,100,89.9,0.0
24,100,93.5,0.1
25,100,87.1,0.0
26,100,90.8,0.2
27,100,92.2,0.1
28,100,89.0,0.0
29,100,90.5,0.1
30,100,91.7,0.2
```