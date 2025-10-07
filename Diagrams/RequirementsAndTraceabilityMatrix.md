# Requirements and Traceability Matrix

## 1. Consolidated Functional Requirements

### Production Requirements
| ID | Requirement | Description | Priority | Measurable Criteria |
|----|------------|-------------|----------|-------------------|
| **FR1** | Core Production Capability | System shall produce pizzas from raw ingredients through automated production line | High | ≤100 pizzas/hour capacity |
| **FR2** | Dough Preparation | System shall prepare and shape dough according to recipes | High | Complete within 5 minutes, Timer verification |
| **FR3** | Sauce Application | System shall apply selected sauce type on prepared dough base | High | ±10ml accuracy, Volume measurement |
| **FR4** | Topping Application | System shall add toppings (cheese, vegetables, meats) per order specifications | High | Weight tolerance ±10g |
| **FR5** | Pizza Baking | System shall bake pizza at specified temperature and duration | High | 250-300°C, Temperature sensors |
| **FR6** | Freezing Capability | System shall optionally freeze pizza for preservation | Medium | -18°C, Temperature verification |
| **FR7** | Half-and-Half Mode | System shall support different toppings on each half of pizza | Medium | UI validation for split configuration |
| **FR8** | Multiple Sauce Types | System shall support variety of sauce options (tomato, white, pesto, BBQ) | Medium | ≥4 sauce types available |
| **FR9** | Cheese Art | System shall create decorative cheese patterns on pizza surface | Low | Pattern recognition validation |
| **FR10** | Calzone Production | System shall optionally fold and seal pizza into calzone format | Low | Fold completeness check |
| **FR11** | Quality Inspection | System shall perform quality checks at key production stages | High | Defect rate <0.1% |

### Web Interface Requirements
| ID | Requirement | Description | Priority | Measurable Criteria |
|----|------------|-------------|----------|-------------------|
| **FR12** | Menu Selection | System shall display menu with predefined pizza options | High | ≥5 menu options |
| **FR13** | Full Customization | System shall allow modification of ingredients, shape, size | High | ≥10 ingredient choices |
| **FR14** | Quantity Selection | System shall allow ordering multiple pizzas in single transaction | High | Accept 1-100 pizza quantity |
| **FR15** | Payment Processing | System shall process payments with multiple payment methods | High | Transaction success rate >99.9% |
| **FR16** | Priority Queue | Custom orders shall receive production priority over standard orders | Medium | Queue metrics verification |
| **FR17** | Order Tracking | System shall provide real-time status updates throughout production | High | Update latency <5 seconds |

### Warehouse Requirements
| ID | Requirement | Description | Priority | Measurable Criteria |
|----|------------|-------------|----------|-------------------|
| **FR18** | Inventory Storage | System shall store raw materials with proper conditions | High | Temperature/humidity monitoring |
| **FR19** | Inventory Monitoring | System shall track ingredient consumption and stock levels | High | Real-time accuracy ±1% |
| **FR20** | Automatic Restocking | System shall automatically reorder when inventory falls below thresholds | High | Trigger within 1 minute of threshold |

## 2. Non-Functional Requirements (Quality Attributes)

| ID | Quality Attribute | Requirement | Measurement | Priority |
|----|------------------|-------------|-------------|----------|
| **NFR1** | Availability | 99% uptime (7.2 hours downtime/month max) | Monitoring logs | High |
| **NFR2** | Performance | Process order in <100ms | Response time metrics | High |
| **NFR3** | Throughput | Handle at least 100 concurrent orders | Load testing results | High |
| **NFR4** | Modifiability | Add new topping/recipe in <2 hours | Time to deploy | Medium |
| **NFR5** | Scalability | Support 2x load increase without degradation | Stress testing | High |
| **NFR6** | Security | Protect user data and payment information | Penetration testing | High |
| **NFR7** | Usability | New user completes order in <3 minutes | User testing | Medium |
| **NFR8** | Reliability | <0.1% production failures | Error logs analysis | High |
| **NFR9** | Maintainability | MTTR (Mean Time To Repair) <30 minutes | Incident reports | Medium |
| **NFR10** | Interoperability | Support REST API integration | API testing | Medium |


## 4. Traceability Matrix

### Functional Requirements Traceability

| Req ID | Requirement | Priority | Feature Model Elements | Analysis Architecture | Design Components |
|--------|------------|----------|----------------------|---------------------|------------------|
| **FR1** | Core Production | High | Production (root) | Production Subsystem, OrderProcessing | OrderProcessingController, All Machine Controllers |
| **FR2** | Dough Preparation | High | DoughCreator, DoughShaper | DoughCreator function | DoughController, DoughService, DoughDispatchService |
| **FR3** | Sauce Application | High | SauceCreator | SauceCreator function | SauceController, SauceService, SauceDispatchService |
| **FR4** | Topping Application | High | ToppingApplication, Cheese/Vegetables/Meats | ToppingApp function | ToppingController, ToppingService, ToppingDispatchService |
| **FR5** | Pizza Baking | High | Cooking, Baking | Cooking function | CookingController, CookingService, CookingDispatchService |
| **FR6** | Freezing | Medium | PostProduction, Freezing | PostProduction function | PackagingController, PackagingService (Freeze:boolean) |
| **FR7** | Half-and-Half | Medium | HalfAndHalfMode | ToppingApp function | ToppingDispatchService, ToppingService |
| **FR8** | Multiple Sauces | Medium | MultipleSauces | SauceCreator function | SauceService (sauce types) |
| **FR9** | Cheese Art | Low | CheeseArt | ToppingApp function | ToppingService (pattern mode) |
| **FR10** | Calzone Production | Low | CalzoneFolding | Cooking function | CookingService (fold mode) |
| **FR11** | Quality Inspection | High | QualityInspection | QualityControl function | QualityController, QualityInspectionService, QualityFeedbackService |
| **FR12** | Menu Selection | High | CustomerInterface, MenuSelection | CustomerInterface function | CustomerInterfaceSW (/menu endpoint) |
| **FR13** | Customization | High | Customization, IngredientSelection, ShapeSelection, SizeSelection | CustomerInterface function | CustomerInterfaceSW, OrderProcessingController |
| **FR14** | Quantity Selection | High | QuantitySelection | CustomerInterface function | CustomerInterfaceSW (validation) |
| **FR15** | Payment | High | Payment | CustomerInterface function, APIGateway | CustomerInterfaceSW, APIGatewaySW |
| **FR16** | Priority Queue | Medium | PriorityQueue | OrderProcessing function | OrderProcessingController (queue logic) |
| **FR17** | Order Tracking | High | OrderTracking | CustomerInterface, KafkaBus | CustomerInterfaceSW, KafkaBusSW (events) |
| **FR18** | Inventory Storage | High | InternalGoodsProvider, ProductionSupply | Warehouse subsystem, GoodsProvider | GoodsProviderController, GoodsProviderService |
| **FR19** | Inventory Monitoring | High | InventoryManagement, UsageTracking, StockMonitoring | InventoryMgmt function, SupplyCoordination | InventoryController, InventoryService |
| **FR20** | Auto Restocking | High | Restocking, AutomaticReordering, SupplierIntegration | Restocking function, SupplyCoordination | RestockingController, RestockingService |

### Non-Functional Requirements Traceability

| Req ID | Quality Attribute | Analysis Realization | Design Implementation | Architectural Tactics |
|--------|------------------|---------------------|----------------------|---------------------|
| **NFR1** | Availability (99%) | Redundant subsystems, KafkaBus reliability | Distributed controllers, Event replay capability | Redundancy, Failover |
| **NFR2** | Performance (<100ms) | Direct function calls, Optimized paths | Timing constraints per component, Async processing | Caching, Parallelism |
| **NFR3** | Throughput (100 concurrent) | Event-driven architecture, Parallel processing | Kafka topics, Horizontal scaling | Load balancing, Queuing |
| **NFR4** | Modifiability (<2hr) | Modular functions, Clear interfaces | Service-based design, Adapter pattern | Loose coupling, Abstraction |
| **NFR5** | Scalability (2x load) | Stateless functions, Message-based | Kafka partitioning, Container orchestration | Horizontal scaling |
| **NFR6** | Security | Gateway pattern, Isolated subsystems | APIGatewaySW security, HTTPS/TLS | Authentication, Encryption |
| **NFR7** | Usability (<3min) | Simple interface flow | CustomerInterfaceSW UX | Progressive disclosure |
| **NFR8** | Reliability (<0.1% fail) | Quality control loops, Monitoring | Error handling, Circuit breakers | Fault detection, Recovery |
| **NFR9** | Maintainability (MTTR <30min) | Clear subsystem boundaries | Event logging, Dashboard monitoring | Observability, Modularity |
| **NFR10** | Interoperability (REST) | API Gateway pattern | REST controllers, OpenAPI spec | Standard protocols |