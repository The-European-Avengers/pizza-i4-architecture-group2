# FeatureModel with Traceability

```
Pizza Manufacturer
├── Production [FR1]
│   ├── ToppingApplication [FR4]
│   │   ├── Cheese
│   │   ├── Vegetables
│   │   ├── Meats
│   │   ├── HalfAndHalfMode [FR7]
│   ├── SauceCreator [FR3]
│   │   └── MultipleSauces [FR8]
│   ├── DoughCreator [FR2]
│   │   └── DoughShaper [FR2]
│   ├── Cooking
│   │   ├── Baking [FR5]
│   │   └── CalzoneFolding [FR9]
│   └── PostProduction
│       ├── Freezing [FR6]
│       ├── QualityInspection [FR10]
│       └── Packaging
├── Warehouse
│   ├── Restocking [FR19]
│   │   ├── AutomaticReordering
│   │   └── SupplierIntegration
│   ├── InventoryManagement [FR18]
│   │   ├── UsageTracking
│   │   └── StockMonitoring
│   └── InternalGoodsProvider [FR17]
│       └── ProductionSupply
└── WebInterface
    ├── CustomerInterface
    │   ├── Ordering
    │   │   ├── MenuSelection [FR11]
    │   │   ├── Customisation [FR12]
    │   │   │   ├── IngredientSelection
    │   │   │   ├── ShapeSelection
    │   │   │   └── SizeSelection
    │   │   ├── QuantitySelection [FR13]
    │   │   └── PriorityQueue [FR15]
    │   ├── Payment [FR14]
    │   ├── OrderTracking [FR16]
    │   └── Authentication
    └── EmployeeInterface
        ├── AnalyticalDashboard
        │   ├── ProductionMetrics
        │   └── QualityMetrics
        ├── SystemManagement
        └── UserManagement
```


![Feature Model](diagram-images/feature-model.png)


