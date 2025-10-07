# FeatureModel with Traceability

```
Pizza Manufacturer
├── Production [FR1]
│   ├── ToppingApplication [FR4]
│   │   ├── Cheese
│   │   ├── Vegetables
│   │   ├── Meats
│   │   ├── HalfAndHalfMode [FR7]
│   │   └── CheeseArt [FR9]
│   ├── SauceCreator [FR3]
│   │   └── MultipleSauces [FR8]
│   ├── DoughCreator [FR2]
│   │   └── DoughShaper [FR2]
│   ├── Cooking
│   │   ├── Baking [FR5]
│   │   └── CalzoneFolding [FR10]
│   └── PostProduction
│       ├── Freezing [FR6]
│       ├── QualityInspection [FR11]
│       └── Packaging
├── Warehouse
│   ├── Restocking [FR20]
│   │   ├── AutomaticReordering
│   │   └── SupplierIntegration
│   ├── InventoryManagement [FR19]
│   │   ├── UsageTracking
│   │   └── StockMonitoring
│   └── InternalGoodsProvider [FR18]
│       └── ProductionSupply
└── WebInterface
    ├── CustomerInterface
    │   ├── Ordering
    │   │   ├── MenuSelection [FR12]
    │   │   ├── Customization [FR13]
    │   │   │   ├── IngredientSelection
    │   │   │   ├── ShapeSelection
    │   │   │   └── SizeSelection
    │   │   ├── QuantitySelection [FR14]
    │   │   └── PriorityQueue [FR16]
    │   ├── Payment [FR15]
    │   ├── OrderTracking [FR17]
    │   └── Authentication
    └── EmployeeInterface
        ├── AnalyticalDashboard
        │   ├── ProductionMetrics
        │   └── QualityMetrics
        ├── SystemManagement
        └── UserManagement
```


