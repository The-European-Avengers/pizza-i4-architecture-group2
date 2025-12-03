# Requirements and Traceability Matrix

## 1. Consolidated Functional Requirements

### Production Requirements

| ID | Requirement | Description | Priority | Measurable Criteria | Timing Constraint |
|----|------------|-------------|----------|-------------------|-------------------|
| **FR1** | Core Production Capability | System shall produce pizzas from raw ingredients through an automated production line | High | **≥100 pizzas/hour capacity** | Event-chain - Input synchronisation constraint |
| **FR2** | Dough Preparation | System shall prepare and shape dough according to recipes | High | Complete within 5 minutes, Timer verification | Event-chain - Input synchronisation constraint |
| **FR3** | Sauce Application | System shall apply selected sauce type on prepared dough base | High | ±10ml accuracy, Volume measurement | Event related - Execution time constraint |
| **FR4** | Topping Application | System shall add toppings (cheese, vegetables, meats) per order specifications | High | Weight tolerance ±10g | Event related - Execution time constraint |
| **FR5** | Pizza Baking | System shall be able to bake pizza at specified temperature and duration | High | 250-300°C, Temperature sensors | Event related - Execution time constraint |
| **FR6** | Freezing Capability | System shall be able to freeze pizza for preservation | Medium | -18°C, Temperature verification | Event related - Execution time constraint |
| **FR7** | Half-and-Half Mode | System shall support different toppings on each half of pizza | Low | UI validation for split configuration | Event related - Arbitrary constraint |
| **FR8** | Multiple Sauce Types | System shall support a variety of sauce options (tomato, white, pesto, BBQ) | Medium | ≥4 sauce types available | Event related - Arbitrary constraint  |
| **FR9** | Calzone Production | System shall optionally fold and seal pizza into calzone format | Low | Fold completeness check | Event related - Arbitrary constraint |
| **FR10** | Packaging | System shall pack pizza in a box | High | Defect rate 5% | Event related - Periodic constraint |

### Web Interface Requirements

| ID | Requirement | Description | Priority | Measurable Criteria | Timing Constraint |
|----|------------|-------------|----------|-------------------|-------------------|
| **FR11** | Menu Selection | System shall display menu with predefined pizza options | High | ≥5 menu options | Event-chain - Reaction constraint |
| **FR12** | Full Customisation | System shall allow modification of ingredients, shape, size | High | ≥10 ingredient choices |  Event-chain - Reaction constraint |
| **FR13** | Quantity Selection | System shall allow ordering multiple pizzas in single transaction | High | Accept 1-100 pizza quantity | Event-chain - Input synchronisation constraint |
| **FR14** | Payment Processing | System shall process payments with multiple payment methods | High | Transaction success rate >99.9% | Event-chain - Input synchronisation constraint |
| **FR15** | Priority Queue | Custom orders shall receive production priority over standard orders | Medium | Queue metrics verification |  Event-chain - Reaction constraint |
| **FR16** | Order Tracking | System shall provide real-time status updates throughout production | High | Update latency <5 seconds | Event-chain - Output synchronisation constraint |

### Warehouse Requirements

| ID | Requirement | Description | Priority | Measurable Criteria | Timing Constraint |
|----|------------|-------------|----------|-------------------|-------------------|
| **FR17** | Inventory Storage | System shall store raw materials with proper conditions | High | Temperature/humidity monitoring | Event-chain - Reaction constraint |
| **FR18** | Inventory Monitoring | System shall track ingredient consumption and stock levels | High | Real-time accuracy ±1% | Event related - Periodic constraint |
| **FR19** | Automatic Restocking | System shall automatically reorder when inventory falls below thresholds | High | Trigger within 1 minute of threshold | Event related - Arbitrary constraint |
| **FR20** | Internal Good Provider | System shall be able to restock production machines with ingredients from warehouse | High | Restock time <10 minutes | Event related - Execution time constraint |

## 2. Non-Functional Requirements (Quality Attributes)

| ID | Quality Attribute | Requirement | Measurement | Priority |
|----|------------------|-------------|-------------|----------|
| **NFR1** | Availability | 99% uptime (7.2 hours downtime/month max) | Monitoring logs | High |
| **NFR2** | Performance | **Produce a pizza <30 seconds** | Response time metrics | High |
| **NFR3** | Throughput | Handle at least 100 concurrent orders | Load testing results | High |
| **NFR4** | Modifiability | Add new topping/recipe in <2 hours | Time to deploy | Medium |
| **NFR5** | Scalability | Support 2x load increase without degradation | Stress testing | High |
| **NFR6** | Security | Protect user data and payment information | Penetration testing | High |
| **NFR7** | Usability | New user completes order in \<3 minutes | User testing | Medium |
| **NFR8** | Reliability | <0.1% production failures | Error logs analysis | High |
| **NFR9** | Maintainability | MTTR (Mean Time To Repair) <30 minutes | Incident reports | Medium |
| **NFR10** | Interoperability | Support REST API integration | API testing | Medium |

## 3. Traceability Matrix

### Functional Requirements Traceability

| Req ID | Requirement | Priority | Feature Model Elements | Analysis Architecture | *Design Components* |
|--------|------------|----------|----------------------|---------------------|------------------|
| **FR1** | Core Production | High | Production | Production Line Components | OrderProcessingController, All Machine Controllers |
| **FR2** | Dough Preparation | High | Dough Machine, Dough Shaper | Dough Machine, Dough Shaper | DoughCreatorController, DoughCreatorService, DoughDispatchService, DoughShaperController, DoughShaperService |
| **FR3** | Sauce Application | High | Sauce Machine | Sauce Machine | SauceController, SauceService, SauceDispatchService |
| **FR4** | Topping Application | High | ToppingApplication, Cheese Grater, Vegetables Slicer, Meat Slicer | Cheese Grater, Vegetables Slicer, Meat Slicer | CheeseController, CheeseService, CheeseDispatchService, MeatController, MeatService, MeatDispatchService, VegetableController, VegetableService, VegetableDispatchService |
| **FR5** | Pizza Baking | High | Cooking, Oven | Oven | OvenController, OvenService, OvenDispatchService |
| **FR6** | Freezing | High | Cooking, Freezing | Freezer | FreezerController, FreezerService, FreezerispatchService |
| **FR7** | Half-and-Half | Low | HalfAndHalfMode | Cheese Grater, Vegetables Slicer, Meat Slicer,  Sauce Machine  | CheeseController, CheeseService, CheeseDispatchService, MeatController, MeatService, MeatDispatchService, VegetableController, VegetableService, VegetableDispatchService |
| **FR8** | Multiple Sauces | Medium | MultipleSauces | Sauce Machine | SauceService (sauce types) |
| **FR9** | Calzone Production | Low | Cooking, CalzoneFolding | - | - |
| **FR10** | Packaging | High | PostProduction, Packaging Robot | Packaging Robot | PackagingController, PackagingService |
| **FR11** | Menu Selection | High | CustomerInterface, Pizza Selection | UI Component | CustomerInterfaceSW (/menu endpoint) |
| **FR12** | Customisation | High | Customisation, IngredientSelection, ShapeSelection, SizeSelection | UI Component | CustomerInterfaceSW, OrderProcessingController |
| **FR13** | Quantity Selection | High | QuantitySelection | UI Component | CustomerInterfaceSW (validation) |
| **FR14** | Payment | High | Payment | UI Component | CustomerInterfaceSW, APIGatewaySW |
| **FR15** | Priority Queue | Medium | PriorityQueue | Order Processing | OrderProcessingController (queue logic) |
| **FR16** | Order Tracking | High | OrderTracking | UI Component | CustomerInterfaceSW, KafkaBusSW (events) |
| **FR17** | Inventory Storage | High | Internal Goods Provider, Production Supply | Internal Goods Provider | GoodsProviderController, GoodsProviderService |
| **FR18** | Inventory Monitoring | High | Inventory Manager, Usage Tracking, StockMonitoring | Inventory Manager | InventoryController, InventoryService |
| **FR19** | Auto Restocking | High | Restocking Handler, Automatic Reordering, Supplier Integration | Restocking Handler | RestockingController, RestockingService |

### Non-Functional Requirements Traceability

| Req ID | Quality Attribute | Analysis Realisation | Design Implementation | Architectural Tactics |
|--------|------------------|---------------------|----------------------|---------------------|
| **NFR1** | Availability (99%) | Redundant subsystems, KafkaBus reliability | Distributed controllers, Event replay capability | Redundancy, Failover |
| **NFR2** | Performance (<100ms) | Direct function calls, Optimised paths | Timing constraints per component, Async processing | Caching, Parallelism |
| **NFR3** | Throughput (100 concurrent) | Event-driven architecture, Parallel processing | Kafka topics, Horizontal scaling | Load balancing, Queuing |
| **NFR4** | Modifiability (<2hr) | Modular functions, Clear interfaces | Service-based design, Adapter pattern | Loose coupling, Abstraction |
| **NFR5** | Scalability (2x load) | Stateless functions, Message-based | Kafka partitioning, Container orchestration | Horizontal scaling |
| **NFR6** | Security | Gateway pattern, Isolated subsystems | APIGatewaySW security, HTTPS/TLS | Authentication, Encryption |
| **NFR7** | Usability (<3min) | Simple interface flow | CustomerInterfaceSW UX | Progressive disclosure |
| **NFR8** | Reliability (<0.1% fail) | Quality control loops, Monitoring | Error handling, Circuit breakers | Fault detection, Recovery |
| **NFR9** | Maintainability (MTTR <30min) | Clear subsystem boundaries | Event logging, Dashboard monitoring | Observability, Modularity |
| **NFR10** | Interoperability (REST) | API Gateway pattern | REST controllers, OpenAPI spec | Standard protocols |
| **NFR11** | Integrability | Well-defined interfaces, Modular design | Service contracts, API documentation | Encapsulation, Standardization, Event-driven integration |

### System Constraints
We have identified the following system constraints that may impact the design and implementation of the pizza production system:
> - **Time Constraints:** For testing we have consider to reduce the time during simulation. So we define a time compression factor of , meaining that 1 second in the experiment would be a 1 minute in real life. So if the pizza production during the experiment takes 20 seconds, in real life it would take 20 minutes.

## Diagrams

### Description of the feature model:

![Feature Model](diagram-images/feature-model.png)

### Analysis level architecture:

![Analysis Level](diagram-images/analysis-level.png)

### Design level components:

![Design Level](diagram-images/design-level.png)