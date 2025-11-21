## I4 Pizza Factory Experiment Documentation

This document serves as the main entry point for the I4 Pizza Factory performance validation project. The documentation is structured modularly for easy maintenance and collaboration.

### 1. Experiment Design and Analysis

This section covers the formal plan, execution, and analysis of the latency experiment, focusing on Non-Functional Requirement (NFR) validation.

* **1.1. Experiment Scope & Design (The Plan):** Detailed breakdown of the **NFR being tested**, the Null/Alternative Hypotheses ($H_{0,lat}, H_{a,lat}$), variables, and statistical design.
    * **Reference:** [ExperimentScope.md](ExperimentExperimentScope.md)

* **1.2. Execution and Automation Scripts:** Step-by-step instructions and scripts to **deploy the microservices**, Kafka topics, and KSQLDB streams/tables. Includes commands for running the experiment and cleaning up the environment.
    * **Reference:** [ExperimentExecution.md](ExperimentExperimentExecution.md)

* **1.3. Data Analysis Techniques:** Overview of statistical methods and tools used to analyze the collected latency data. Includes how to access the **Order Latency** and **Pizza Latency** data via the FastAPI endpoints.
    * **Reference:** [DataAnalysis.md](Experiment/DataAnalysis.md)

### 2. System Implementation and Technology Stack

This section details the specific **polyglot microservices** implementation used in the experiment and their respective roles, technology, and team responsibilities.

* **2.1. System Overview & Technology Stack:** Quick start instructions, folder structure, and the **specific programming languages** (C\#, Python, Go) used for each machine.
    * **Reference:** [ExperimentImplementation.md](ExperimentExperimentImplementation.md) - Folder Structure and Technology Stack

* **2.2. Kafka Topics and Kafka Messages Structure:** Documentation of all Kafka topics used in the system, including their purpose and the **JSON schema** for messages exchanged between services.
    * **Reference:** [ExperimentImplementation.md](ExperimentExperimentImplementation.md) - Topics and Message Schemas

* **2.3. KSQLDB Streams and Tables:** Definitions and schemas for all KSQLDB streams and tables used to process and analyze real-time data during the experiment.
    * **Reference:** [ExperimentImplementation.md](ExperimentExperimentImplementation.md) - KSQLDB Streams and Tables

### 3. Data and Configuration

This file acts as the configuration definition for the production environment, specifying all ingredients and recipes.

* **3.1. Pizza Menu and Recipe Structure:** Defines the standardized **JSON object structure** used for all recipes (`name`, `sauce`, `cheese`, `meat`, `veggies`).
    * **Reference:** [PizzaMenu.md](Experiment/PizzaMenu.md) - Data Structure Overview

* **3.2. Ingredient Inventory:** Comprehensive list of all **Sauces**, **Cheeses**, **Meats**, and **Vegetables** recognized by the automated dispensing system.
    * **Reference:** [PizzaMenu.md](Experiment/PizzaMenu.md) - Ingredient Inventory