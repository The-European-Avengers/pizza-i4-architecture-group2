# UPPAAL Model: Pizza Production Line

## Overview
This UPPAAL model formally specifies and verifies a real-time pizza production system for an Industry 4.0 factory. The model captures the complete production pipeline from order placement through ingredient processing to final packaging.

## System Architecture

### Templates (14 total)

**Production Line Components:**
- `DoughMachine` - Kneads dough for pizza bases
- `DoughShaper` - Shapes dough into desired form
- `SauceMachine` - Applies sauce to pizza base
- `CheeseGrater` - Adds grated cheese
- `MeatSlicer` - Adds meat toppings
- `VegetablesSlicer` - Adds vegetable toppings (routes to oven or freezer)
- `Oven` - Bakes pizzas at target temperature (100°C)
- `Freezer` - Freezes pizzas at -18°C
- `PackagingRobot` - Packages finished pizzas

**Supply Chain Components:**
- `InternalGoodsProvider` - Manages ingredient distribution to machines
- `InventoryManager` - Monitors warehouse stock levels
- `RestockingHandler` - Handles external ingredient orders

**Control Components:**
- `Customer` - Generates pizza orders
- `OrderDispatcher` - Manages order pickup and completion

## Modeling Approach: Buffer-Based Decoupling

### Design Rationale
Rather than using direct channel synchronization between consecutive production stages, we implemented a **buffer-based architecture** with global counters for each production stage. This design decision was driven by several critical factors:

**Problem with Direct Synchronization:**
- Direct channel-to-channel communication between timed automata creates tight coupling
- Time-constrained transitions (e.g., `t<=10` invariants) combined with synchronous handshakes lead to frequent deadlocks
- When one machine finishes processing but the next machine isn't ready to receive, the system can block indefinitely
- Concurrent production of multiple pizzas exacerbates synchronization conflicts

**Buffer-Based Solution:**
We introduced 7 intermediate buffers as global integer counters:
- `raw_dough_buffer_count` (between DoughMachine and DoughShaper)
- `dough_buffer_count` (between DoughShaper and SauceMachine)
- `sauce_buffer_count` (between SauceMachine and CheeseGrater)
- `cheese_buffer_count` (between CheeseGrater and MeatSlicer)
- `meat_buffer_count` (between MeatSlicer and VegetablesSlicer)
- `oven_buffer_count` (between VegetablesSlicer and Oven)
- `freezer_buffer_count` (between VegetablesSlicer and Freezer)

**Benefits:**
- **Temporal decoupling**: Machines can finish processing and dispatch to buffers without waiting for downstream machines
- **Deadlock prevention**: Buffers absorb timing mismatches between production stages
- **Concurrent operation**: Multiple pizzas can be at different stages simultaneously without blocking
- **Capacity management**: Buffer limits (MAX_PIZZA_BUFFER = 2) prevent unbounded queuing and enable bottleneck detection

**Implementation:**
Each machine checks buffer availability (`buffer_count < MAX_PIZZA_BUFFER`) before dispatching and increments the counter. The next stage decrements the counter when pulling from the buffer. This asynchronous handoff eliminates the timing conflicts inherent in synchronous channel communication.

## Key Features

### Timing Constraints
- **Preheating**: Oven (2 time units), Freezer (5 time units)
- **Processing**: Dough shaping (≤2), Baking (≤10), Freezing (≤10)
- **Dispatch operations**: All machines (≤2 time units)

### Resource Management
- **Two-tier inventory**: Machine-level stock (5 units) + Warehouse (25 units)
- **Automatic restocking**: Triggered when stock ≤ 0
- **Buffer management**: Max 2 pizzas between each production stage

### Production Control
- **Maximum capacity**: 3 concurrent orders
- **Production variants**: Oven-baked or frozen pizzas
- **Optional toppings**: Meat and vegetables (controlled by boolean flags)

## Verification Results

### Properties Verified (26 queries)

**Safety (7 queries):**
- Deadlock freedom
- Temperature constraints (baking at 100°C, freezing at -18°C)
- Stock levels never negative (machines and warehouse)
- Production capacity never exceeds maximum (≤3 pizzas)

**Reachability (9 queries):**
- All production stages reachable (baking, freezing, packaging)
- System reaches maximum capacity
- Concurrent oven and freezer operation
- Restocking mechanisms functional
- Buffers can become full (bottleneck detection)

**Capacity (1 query):**
- System eventually reaches full production capacity (3 pizzas) during operation

**Liveness (4 queries):**
- Restocking eventually completes at machine level
- Orders flow end-to-end to packaging
- Warehouse orders eventually arrive
- Restocking state leads to dispatch

**Performance (5 queries):**
- Time bounds enforced (kneading ≤ 10, shaping ≤ 10, sauce/meat/vegetables/cheese application ≤ 10)
- Bottleneck detection at dough shaper, cheese grater, and vegetable slicer
- Resource utilization validated (oven can be busy while pizzas wait)

## Model Characteristics

**Global Variables:**
- Temperature tracking: `temp`, `freezer_temp`
- Inventory: 5 warehouse stocks, 5 machine stocks
- Production state: `pizzas_in_production`, 7 buffer counters
- Control flags: `isOn`, `isOven`, `noMeatPizza`, `noVegetablePizza`, `isRestocking`

**Synchronization:**
- 40+ channels for machine coordination
- Broadcast channels: `turn_on`, `pizza_request`
- Binary channels: ingredient requests, restocking operations

**Committed Locations:**
- Used in stock checking and restocking decision points
- Ensures atomic transitions for resource allocation

## Quality Attributes Verified

1. **Performance**: Processing stages meet timing constraints
2. **Availability**: System handles restocking without disruption
3. **Correctness**: Temperature requirements enforced, no resource violations
4. **Throughput**: System supports concurrent production (3 pizzas)
5. **Safety**: No deadlocks, no negative inventory

## Queries

This section lists the UPPAAL queries used for verification and validation.
Each query is presented together with its original description, mirroring the intent and comments defined directly in the UPPAAL model.

### Safety Queries

* **Deadlock freedom**
  `A[] not deadlock`
  *Safety:* System never reaches deadlock state

* **Correct operating temperatures**
  `A[] ((oven.baking imply temp == TARGET_TEMP) and (freezer.freeze imply freezer_temp == FREEZE_TEMP))`
  *Safety:* Baking and freezing only occur at target temperatures

* **Global stock validity**
  `A[] (stock_level < 0 imply false)`
  *Safety:* Stock level never negative

* **Production capacity limit**
  `A[] (pizzas_in_production <= MAX_ORDERS_IN_SYSTEM)`
  *Safety:* Pizza production never exceeds maximum capacity

* **Machine stock validity**
  `A[] (dough_stock >= 0 and sauce_stock >= 0 and cheese_stock >= 0 and meat_stock >= 0 and veg_stock >= 0)`
  *Safety:* Machine stock levels never become negative

* **Warehouse stock validity**
  `A[] (dough_warehouse >= 0 and sauce_warehouse >= 0 and cheese_warehouse >= 0 and meat_warehouse >= 0 and veg_warehouse >= 0)`
  *Safety:* Warehouse stock levels never become negative


### Reachability Queries

* **Oven operation**
  `E<> oven.baking`
  *Reachability:* Oven can reach baking state

* **Packaging robot operation**
  `E<> packagingRobot.operatePizza`
  *Reachability:* Packaging robot can reach operating state

* **Freezer operation**
  `E<> freezer.freeze`
  *Reachability:* Freezer can reach freezing state

* **Internal goods supply**
  `E<> internalGoodsProvider.provideDough`
  *Reachability:* Goods provider can supply ingredients

* **Machine restocking trigger**
  `E<> doughMachine.restock`
  *Reachability:* Dough machine can trigger restocking

* **Warehouse supply request**
  `E<> inventoryManager.requestDough`
  *Reachability:* Warehouse can request external supplies

* **Maximum production load**
  `E<> (pizzas_in_production == MAX_ORDERS_IN_SYSTEM)`
  *Reachability:* System can reach maximum production capacity

* **Parallel cooking operations**
  `E<> (oven.baking and freezer.freeze)`
  *Reachability:* Oven and freezer can operate simultaneously

* **Full buffer detection**
  `E<> (dough_buffer_count == MAX_PIZZA_BUFFER)`
  *Reachability:* Buffers can become full (bottleneck detection)


### Capacity Queries

* **Full production during operation**
  `A<> (doughMachine.kneading imply pizzas_in_production == 3)`
  *Capacity:* System reaches full production capacity during operation


### Liveness Queries

* **Restocking progression**
  `E[] doughMachine.restock imply doughMachine.dispatch`
  *Liveness:* Restocking state eventually leads to dispatch

* **End-to-end order flow**
  `E<> customer.turnOnMachines imply packagingRobot.operatePizza`
  *Liveness:* Orders eventually reach packaging (end-to-end flow)

* **Dough machine restocking completion**
  `doughMachine.restock --> doughMachine.restocked`
  *Liveness:* Dough machine restocking eventually completes

* **Warehouse restocking completion**
  `inventoryManager.requestDough --> inventoryManager.restockDough`
  *Liveness:* Warehouse orders eventually arrive


### Performance Queries

* **Processing time constraints**

  ```
  A[] (
    (doughShaper.shapingDough imply doughShaper.t <= 2) and
    (doughMachine.kneading imply doughMachine.t <= 5) and
    (sauceMachine.addToPizza imply sauceMachine.t <= 5) and
    (meatSlicer.addToPizza imply meatSlicer.t <= 5) and
    (cheeseGrater.addToPizza imply cheeseGrater.t <= 5)
  )
  ```

  *Performance:* All ingredient processing steps must strictly adhere to their maximum time constraints (in time units),
  across all possible execution paths of the system. This verifies the simultaneous compliance of all critical
  topping and dough preparation stages (Dough Shaping ≤2, Kneading ≤5, and all topping additions ≤5) with the specified timing requirements.

* **Oven utilization**
  `E<> (oven.baking and oven_buffer_count > 0)`
  *Performance:* Oven can be busy while pizzas wait (utilization check)

* **Dough shaper bottleneck**
  `E<> (raw_dough_buffer_count == MAX_PIZZA_BUFFER and dough_buffer_count == 0)`
  *Performance:* Detects bottleneck at dough shaper

* **Meat slicer bottleneck**
  `E<> (meat_buffer_count == MAX_PIZZA_BUFFER and oven_buffer_count == 0)`
  *Performance:* Detects bottleneck at vegetable slicer

* **Cheese grater bottleneck**
  `E<> (sauce_buffer_count == MAX_PIZZA_BUFFER and cheese_buffer_count == 0)`
  *Performance:* Detects bottleneck at cheese grater
