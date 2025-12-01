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
- `Freezer` - Freezes pizzas
- `PackagingRobot` - Packages finished pizzas

**Supply Chain Components:**
- `InternalGoodsProvider` - Manages ingredient distribution to machines
- `InventoryManager` - Monitors warehouse stock levels
- `RestockingHandler` - Handles external ingredient orders

**Control Components:**
- `Customer` - Generates pizza orders
- `OrderDispatcher` - Manages order pickup and completion

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
- Time bounds enforced (shaping ≤10)
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

**Committed States:**
- Used in stock checking and restocking decision points

## Quality Attributes Verified

1. **Performance**: Certain processing stages meet timing constraints
2. **Availability**: System handles restocking without disruption
3. **Correctness**: Temperature requirements enforced, no resource violations
4. **Throughput**: System supports concurrent production (3 pizzas)
5. **Safety**: No deadlocks, no negative inventory

