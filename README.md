# Advanced Software Architecture and Analysis Techniques

This repository contains the course materials, implementation, and documentation for the Advanced Software Architecture and Analysis Techniques master's course. The project demonstrates a complete software system for pizza production with event-driven architecture, formal verification, and comprehensive analysis.

## Table of Contents

- [Repository Structure](#repository-structure)
- [Documentation](#documentation)
- [Implementation](#implementation)
- [Getting Started](#getting-started)
- [Technologies Used](#technologies-used)
- [Contribution](#contribution)

## Repository Structure

```
├── Docs/                    # Complete project documentation
├── src/                     # Source code implementation
│   ├── experiment/          # Pizza production experiment
│   ├── data-analysis/       # Data analysis and visualization
│   ├── kafka/               # Kafka configuration and setup
│   └── web/                 # Web applications
├── report/                  # LaTeX report templates
├── uppaal/                  # Formal verification models
└── images/                  # Project diagrams and assets
```

## Documentation

All project documentation is located in the **`Docs/`** directory:

### Architecture Documentation

TODO: add link to files so are clickable in github

- **[FeatureModel.md](./Docs/FeatureModel.md)** - Feature model and variability analysis
- **[RequirementsAndTraceabilityMatrix.md](./Docs/RequirementsAndTraceabilityMatrix.md)** - System requirements and traceability
- **[StateMachines.md](./Docs/StateMachines.md)** - State machine specifications for system components
- **[ExperimentCommunicationArchitecture.md](./Docs/ExperimentCommunicationArchitecture.md)** - Communication architecture design
- **[UppaalDescription.md](./Docs/UppaalDescription.md)** - Formal verification specifications
- **[verification_validation.md](./Docs/verification_validation.md)** - V&V strategy and results

### Experiment Documentation

Located in **`Docs/Experiment/`**:

- **[PizzaProductionExperiment.md](./Docs/Experiment/PizzaProductionExperiment.md)** - Overview of the experiment
- **[ExperimentScope.md](./Docs/Experiment/ExperimentScope.md)** - Scope and objectives
- **[ExperimentImplementation.md](./Docs/Experiment/ExperimentImplementation.md)** - Implementation details
- **[ExperimentExecution.md](./Docs/Experiment/ExperimentExecution.md)** - Execution procedures
- **[PizzaMenu.md](./Docs/Experiment/PizzaMenu.md)** - Pizza recipes and configurations
### Diagrams

All architectural diagrams are available in:

- **`Docs/diagram-images/`** - PNG exports of diagrams

  - Analysis-level architecture
  - Design-level architecture
  - Feature model
  - State machines for all components
  - Subsystems tree
  - Experiment communication flow

- **`Docs/diagram-scripts/`** - Source files for diagrams
  - `.excalidraw` files for editing
  - `.dot` files for Graphviz diagrams

### Additional Documentation

- **[Contribution.md](./Docs/Contribution.md)** - Contribution guidelines
- **[ReportStructure.md](./Docs/ReportStructure.md)** - Report organization guide
- **[Peer_doc_group_2.md](./Docs/Peer_doc_group_2.md)** - Peer review documentation made for group 2

## Implementation

### Pizza Production Experiment (`src/experiment/`)

A complete event-driven pizza production system built with microservices:

#### Core Components

- **Order Processing** (`order-processing/`) - C# service for order management
- **Warehouse** (`warehouse/`) - Python services for inventory and order dispatching
  - `internal_goods_provider/` - Inventory management
  - `order_dispatcher/` - Order routing

#### Production Machines

Each machine is implemented as an independent service:

- **Dough Processing**
  - `dough-machine/` - C# service for dough preparation
  - `dough-shaper/` - C# service for shaping
- **Topping Processing**
  - `Sauce/` - Python sauce application service
  - `Meat/` - Go meat slicing service
  - `cheese/` (CheeseGrater) - C# cheese preparation service
  - `Vegetables/` - Go vegetable slicing service
- **Cooking & Packaging**
  - `Oven/` - Python baking service
  - `Freezer/` - Python freezing service
  - `Packaging/` - Go packaging robot service

#### Infrastructure

- **Kafka** - Event streaming backbone
- **KsqlDB** (`KsqlDB/`) - Stream processing and analytics
- **Kafka Initializer** (`kafka-initializer/`) - C# service for topic setup
- **API Gateway** (`Api/`) - Python REST API interface

#### Docker Support

- **[docker-compose.yml](./src/experiment/docker-compose.yml)** - Complete system orchestration
- Individual Dockerfiles for each service

### Web Applications (`src/web/`)

Two interconnected web applications:

#### Client Gateway (`client-gateway/`)

- **Technology**: NestJS (TypeScript)
- **Purpose**: API gateway and request routing
- **Modules**:
  - Authentication integration
  - Order management
  - Order stack management
  - User management
- **Pattern**: Microservices communication via RPC

#### Ordering Microservice (`ordering-ms/`)

- **Technology**: NestJS (TypeScript)
- **Purpose**: Order processing and lifecycle management
- **Modules**:
  - Order creation and tracking
  - Menu management
  - Integration with Client Gateway

### Data Analysis (`src/data-analysis/`)

Comprehensive analysis of experiment results:

- **Jupyter Notebook**:

  - [analysis.ipynb](./src/data-analysis/analysis.ipynb) - Data analysis and visualization

- **Experiment Data** (`experiment-data/`):

  - Order latency measurements
  - Pizza production time tracking
  - Machine restock latency for all components
  - Order dispatch metrics

- **Generated Reports**:
  - Statistical analysis CSVs
  - Visualization charts (PNG)
  - Efficiency metrics

### Kafka Setup (`src/kafka/`)

Basic Kafka infrastructure:

- Producer and consumer examples
- Docker Compose configuration
- Python virtual environment setup

### Scripts (`src/web/scripts/`)

Utility scripts for testing:

- **[newOrders.ts](./src/web/scripts/newOrders.ts)** - Order generation script
- **[script.sh](./src/web/scripts/script.sh)** - Automation helper

## Experiment Replication Guide

### Prerequisites

- Docker and Docker Compose
- Node.js (v18+) and npm
- Python (v3.8+) with Jupyter Notebook

### Running the Pizza Production System

1. **Navigate to the experiment directory**:

   ```bash
   cd src/experiment
   ```

2. **Start all services**:

   ```bash
   docker-compose up -d
   ```

3. **Initialize Kafka topics**:
   ```bash
   # Wait for Kafka to be ready, then run initializer
   docker-compose logs -f kafka-initializer
   ```

### Running the Web Applications

1. **Start client gateway**:
   ```bash
   cd src/web/client-gateway
   npm install
   npm run start:dev
   ```
2. **Running an Order (Load Generation)**

The load generation scripts (`script.sh` and `newOrder.ts`) are located in the `src/web/scripts` folder. Navigate to the directory containing the load script:

```bash
cd src/web/scripts
```

The `script.sh` is configured to execute `newOrder.ts` multiple times, simulating the **10 replications** required for the experiment. The script must be edited to target the correct load level (10, 50, or 100 pizzas). Then run the bash script to execute the test cell:

```bash
./script.sh
```

### Running Data Analysis

Open the data analysis notebook with a Jupyter environment:

```bash
cd src/data-analysis
jupyter notebook analysis.ipynb
```

### View Kafka Topics and Messages

To inspect Kafka topics and messages, we have used Redpanda Console, which can be accessed at `http://localhost:8090` after starting the Docker Compose setup. Be sure to check Redpanda container is running. This tool provides a user-friendly interface to monitor and manage Kafka topics, producers, and consumers. So, during the experiment execution, you can observe the message flow and topic statistics in real-time.

### Languages

- **C#** - .NET 8.0 microservices
- **Python** - Machine services and data analysis
- **Go** - High-performance machine services
- **TypeScript** - Web applications and gateway

### Frameworks & Libraries

- **NestJS** - Backend microservices framework
- **Pandas/NumPy** - Data analysis

### Infrastructure

- **Apache Kafka** - Event streaming platform
- **KsqlDB** - Stream processing
- **Docker** - Containerization
- **PostgreSQL** - Relational database

### Formal Methods

- **UPPAAL** - Timed automata verification

## Formal Verification

UPPAAL models are located in **`uppaal/`**:

- **`pizzaProduction.xml`** - Complete system model
  - Timed automata for all components
  - Properties verification
  - Deadlock analysis
  - Timing constraint validation

Refer to [[Docs/UppaalDescription.md]] for model details and verification results.

## Contribution

Below is a summary of each member's contributions to the project:

| Task                     | Jonathan                                                     | Jeremy                                         | Artem                                                                           | Miguel                                                                                                                      | Sagor       |
| ------------------------ | ------------------------------------------------------------ | ---------------------------------------------- | ------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ----------- |
| Requirements & Use Cases | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           |             |
| Feature Model            | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           |             |
| Design Model             | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           |             |
| Analysis Model           | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           |             |
| State Machines           | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           |             |
| First Pitch              | Contributor & Presenter                                      | Contributor & Presenter                        | Contributor                                                                     | Contributor                                                                                                                 |             |
| Traceability Matrix      | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           |             |
| Exercise 8               | ✓                                                            | ✓                                              |                                                                                 |                                                                                                                             |             |
| UPPAAL                   |                                                              |                                                | Main Developer                                                                  | Helper                                                                                                                      |             |
| Experiment               | Kafka deployment, Order Dispatcher & Internal Goods Provider | API Gateway, Customer microservice & design DB | Kafka Initializer, Order Processing, Dough Machine, Dough Shaper, Cheese Grater | Meat Machine, Sauce Machine, Vegetable Slicer, Oven, Freezer, Packaging Robot, KsqlDB, API to retrieve data & Data Analysis |             |
| Second Pitch             | Contributor                                                  | Contributor                                    | Contributor & Presenter                                                         | Contributor & Presenter                                                                                                     | Contributor |
| Report                   | ✓                                                            | ✓                                              | ✓                                                                               | ✓                                                                                                                           | ✓           |
