# Deliverable 1: Experiment Design Document

This document formalizes the plan for testing the performance and latency of the I4 Pizza Factory's event-driven microservice architecture.

## 1. Quality Attribute and Goal Definition

### 1.1. Selected Quality Attribute (NFR)
The experiment validates the key performance Non-Functional Requirement (NFR) for the Kafka-based architecture:
* **NFR2 (Performance/Latency):** Process order submission time in less than 100 milliseconds ($< 100ms$).

### 1.2. Experiment Goal and Context
* **Goal:** To determine if the system's latency performance meets NFR2 under varying simulated order loads.
* **Perspective:** System Architect, confirming the acceptability of the chosen asynchronous architecture's latency.
* **Context:** Testing will occur in an off-line test environment using simulated order loads (batches of pizzas) to specifically validate the event-based Kafka implementation.


## 2. Hypothesis Formulation

We measure the system's **user-facing response time** when submitting a high-load order (100 pizzas) against the $100ms$ target.

* **Null Hypothesis ($H_{0,lat}$):** The system's average user-facing response time ($\mu_{lat}$) for an order of 100 pizzas is greater than or equal to the $100ms$ target.
    $$H_{0,lat}: \mu_{lat} \ge 100ms$$
* **Alternative Hypothesis ($H_{a,lat}$):** The system's average user-facing response time for an order of 100 pizzas is less than the $100ms$ target.
    $$H_{a,lat}: \mu_{lat} < 100ms$$


## 3. Variables and Design

### 3.1. Independent Variable (Factor)
The factor tested is the **Order Load (Pizzas)**, representing the number of pizzas produced in a single test burst to observe latency behavior under stress.

| Level | Description |
| :--- | :--- |
| **Unit Load** | 1 pizza |
| **Low Load** | 10 pizzas |
| **Medium Load** | 50 pizzas |
| **High Load** | 100 pizzas |

### 3.2. Dependent Variables (Metrics)
1.  **D1: User-Facing Response Time (ms):** The time from the "customer program" producing the order message to Kafka until the confirmation is received. This directly measures NFR2.

### 3.3. Experiment Design
This is a **one-way experiment design** with 4 levels of the independent variable.
* **Blocking:** All tests will be run on the same hardware configuration (local machine running Docker).


## 4. Instrumentation and Validity

### 4.1. Measurement Instruments
> * **Load Generator:** **JMeter** will simulate the "customer program" using its Kafka plugin to produce order messages directly to the Kafka topic at the specified load levels.
> * **Metrics Capture:** **JMeter** automatically logs the average **Response Time (D1)** and **Error Rate (D2)** for each run.
> * **System Builds:** The `master` branch build will be used, deploying all services (Go, Python, C\#) as Kafka consumers/producers.
> * **Test Environment:** A local machine running **Docker** with the Kafka cluster.

### 4.2. Validity Evaluation
>* **Internal Validity:**
>    * *Threat:* Kafka configuration (e.g., partition count, replication factor) can heavily influence performance.
>    * *Mitigation:* Document the exact, constant Kafka configuration used (e.g., "3 partitions, replication factor 1") for all runs.
>* **Construct Validity:**
>    * *Threat:* Is "Kafka produce time" the true user-facing latency?
>    * *Mitigation:* Yes, the user's "submit" action is complete once the order is successfully accepted by Kafka. This is the correct metric for NFR2.
>* **External Validity:**
>    * *Threat:* A single-node Docker Compose environment may not replicate the performance of a production Kubernetes cluster.
>    * *Mitigation:* Accepted. The goal is to obtain a logical baseline; results will be documented as "valid for the test environment".

## 5. System Implementation Responsibilities

This section outlines the team members and technologies responsible for developing the services deployed in the test environment.

| Engineer | Implemented Services | Technologies/Languages |
| :--- | :--- | :--- |
| **Artem** | Dough Machine, Dough Shaper, Cheese Grater, Order Processor | C\# |
| **Miguel** | Vegetables Slicer, Meat Slicer, Packaging-robot, Sauce Machine, Oven, Freezer, Data Retrieval (FastAPI), Real-time Kafka Monitoring (ksqlDB) | Go, Python, FastAPI, KSQLDB |
| **Jonathan** | Internal Good Provider, Kafka Bus Configuration | Python, Kafka |
| **Jeremy** | Frontend, API Gateway, Authentication MS, Ordering MS | TypeScript, Node.js |

