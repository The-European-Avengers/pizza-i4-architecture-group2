# Experiment Execution and Automation

This document provides the step-by-step instructions and commands necessary to deploy, execute, and reset the I4 Pizza Factory latency experiment using Docker, Kafka (Redpanda), and the necessary Node.js services.


## 1\. System Deployment and Startup

### 1.1. Core Service Deployment (Docker)

All core services (Kafka, KSQLDB, and polyglot machines) are containerized. Execute the following command from the root directory containing the `docker-compose.yml` file. Use the **`--build`** flag initially or after code changes.

```bash
docker-compose up --build
```
  * **Sequence:** The **`kafka-init`** service creates all required Kafka topics, then the production line services start up (e.g., `dough-machine`, `order-processor`).
  * **Ready State:** Wait for all logs to confirm services are **"subscribed"** and **"ready"**.

### 1.2. API Gateway Deployment (Node.js)

The **API Gateway** and **Ordering MS** are part of the web components. They must be run outside of Docker (or in a separate container/folder) to expose the order submission endpoint.

> To deploy the API Gateway is necessary to have **Node.js** and **npm** installed. Also, a `env` file with the required environment variables must be created in the `src/web/client-gateway` folder.

1.  Navigate to the appropriate project folder for the API Gateway:
    ```bash
    cd src/web/client-gateway
    ```
2.  Start the API Gateway service in development mode:
    ```bash
    npm run start:dev
    ```
    Wait for the service to report it is running and accessible (e.g., via `http://localhost:3000`).


## 2\. Running an Order (Load Generation)

Order submissions must go through the **API Gateway** and **Ordering MS** to simulate the full user-facing latency. Instead of direct `curl` commands, load is generated via a `newOrder.ts` script executed by `script.sh`.

### 2.1. Locating the Load Script

The load generation scripts (`script.sh` and `newOrder.ts`) are located in the `src/web/scripts` folder.

1.  Navigate to the directory containing the load script:
    ```bash
    cd src/web/scripts
    ```

### 2.2. Executing the Experiment Load

The `script.sh` is configured to execute `newOrder.ts` multiple times, simulating the **10 replications** required for the experiment. The script must be edited to target the correct load level (10, 50, or 100 pizzas).

Run the bash script to execute the test cell:

```bash
./script.sh
```

| Level| Description |
| --- | --- |
| Unit Load | 1 pizza |
| Low Load | 10 pizzas |
| Medium Load | 50 pizzas |
| High Load | 100 pizzas |


## 3\. Data Analysis and Extraction




Latency data is captured and calculated by KSQLDB and exposed via the FastAPI service, allowing you to easily query and extract the latency tables in real-time using standard HTTP requests.

### Base URL

When running locally with Docker Compose, the API service is typically accessible on port `8000` (or as defined in your `docker-compose.yml`).

```
http://localhost:8000/ksql/
```

### Available Endpoints

There are one endpoint per KSQLDB table, allowing you to retrieve data in CSV format. Each one retrieves the data as plain text with the correct `text/csv` header, making it easy to copy/paste or pipe the output into files or other tools.

The available tables are:
* `order_latency`: Contains latency data for entire orders.
* `pizza_latency`: Contains latency data for individual pizzas.
* `dough_machine_restock_latency`: Contains latency data for dough machine restocking events.
* `sauce_machine_restock_latency`: Contains latency data for dough machine restocking events.
* `cheese_machine_restock_latency`: Contains latency data for dough machine restocking events.
* `meat_machine_restock_latency`: Contains latency data for dough machine restocking events.
* `vegetables_machine_restock_latency`: Contains latency data for dough machine restocking events.
* `packaging_machine_restock_latency`: Contains latency data for dough machine restocking events.

### Querying Data
The endpoints follow this pattern:

```
GET /ksql/{table_name}/csv
```

#### Example Request:

```
GET http://localhost:8000/ksql/order_latency/
```

#### Response Example: 

```csv
ORDERID,ORDERSIZE,STARTTIMESTAMP,ENDTIMESTAMP,LATENCYMS
10,5,1763308900000,1763308950000,50000
11,10,1763308910000,1763308975000,65000
```


## 4\. Running a Clean Experiment Replication

To ensure the integrity of the latency measurements for the 10 planned replications per load level, all persistent Kafka data must be cleared between runs.

### 4.1. Stop and Clean Command

1.  Stop the API Gateway service (Ctrl+C).

2.  Stop the core Docker services and remove the persistent volume:

    ```bash
    docker-compose down -v
    ```

After this command finishes, return to **Step 1.1** to start a fresh run.
