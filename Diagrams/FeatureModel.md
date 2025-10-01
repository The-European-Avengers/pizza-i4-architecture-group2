# FeatureModel

## Requirements Table for Pizza Production System

| ID | Requirement | Description | Priority |
|---|---|---|---|
| **Req#1_CoreProduction** | The system shall produce pizza | Core capability to manufacture pizza from raw ingredients through automated production line | High |
| **Req#2_DoughPreparation** | The system shall prepare dough | Automated mixing and preparation of pizza dough according to recipes | High |
| **Req#3_SauceApplication** | The system shall add sauce over dough | Precise application of selected sauce type on prepared dough base | High |
| **Req#4_ToppingApplication** | The system shall add toppings | Automated placement of cheese, vegetables, and meat toppings per order specifications | High |
| **Req#5_Cooking** | The system shall cook pizza | Baking pizza at specified temperature and duration based on type | High |
| **Req#6_Freezing** | The system shall freeze pizza | Optional freezing capability for preservation and frozen product line | Medium |
| **Req#7_MenuSelection** | The system shall allow menu selection | Customer can choose from predefined pizza options | High |
| **Req#8_Customization** | The system shall allow full customization | Customer can modify ingredients, shape, size, and create custom pizzas | High |
| **Req#9_HalfAndHalf** | The system shall support half-and-half pizzas | Different toppings on each half of the pizza | Medium |
| **Req#10_QuantitySelection** | The system shall allow quantity selection | Customer can order multiple pizzas in single transaction | High |
| **Req#11_MultipleSauces** | The system shall support multiple sauce types | Variety of sauce options (tomato, white, pesto, BBQ) | Medium |
| **Req#12_CheeseArt** | The system shall draw pictures using cheese | Decorative cheese patterns/designs on pizza surface | Low |
| **Req#13_PaymentProcessing** | The system shall handle payments | Secure payment processing with multiple payment methods | High |
| **Req#14_InventoryStorage** | The system shall store raw materials | Warehouse management for ingredients with proper conditions | High |
| **Req#15_PriorityQueue** | The system shall prioritize custom pizzas | Custom orders receive production priority over standard orders | Medium |
| **Req#16_CalzoneProduction** | The system shall produce calzones (optional) | Capability to fold and seal pizza into calzone format | Low |
| **Req#17_OrderTracking** | The system shall provide order tracking | Real-time status updates throughout production and delivery | High |
| **Req#18_InventoryMonitoring** | The system shall monitor inventory levels | Continuous tracking of ingredient consumption and stock levels | High |
| **Req#19_AutoRestocking** | The system shall automate restocking | Automatic reordering when inventory falls below thresholds | High |
| **Req#20_QualityControl** | The system shall perform quality checks | Inspection at key production stages to ensure standards | High |

## Enhanced Feature Model Tree with Traceability

```
Pizza Manufacturing System
├── «feature» Production [Req#1]
│   ├── «feature» DoughProcessing [Mandatory]
│   │   ├── «feature» DoughMaking [Req#2]
│   │   └── «feature» DoughShaping [Req#2]
│   ├── «feature» ToppingStation [Mandatory]
│   │   ├── «feature» SauceApplication [Req#3, Req#11]
│   │   │   ├── «feature» TomatoSauce
│   │   │   ├── «feature» WhiteSauce
│   │   │   └── «feature» SpecialtySauces [Optional]
│   │   ├── «feature» CheeseApplication [Req#4]
│   │   │   ├── «feature» StandardCheese
│   │   │   └── «feature» CheeseArtwork [Req#12] [Optional]
│   │   └── «feature» ToppingApplication [Req#4]
│   │       ├── «feature» Vegetables
│   │       ├── «feature» Meats
│   │       └── «feature» HalfAndHalfMode [Req#9] [Optional]
│   ├── «feature» Cooking [Mandatory]
│   │   ├── «feature» Baking [Req#5]
│   │   └── «feature» CalzoneFolding [Req#16] [Optional]
│   └── «feature» PostProduction [Mandatory]
│       ├── «feature» Freezing [Req#6] [Optional]
│       ├── «feature» QualityInspection [Req#20]
│       └── «feature» Packaging
│
├── «feature» Warehouse [Req#14]
│   ├── «feature» InventoryManagement [Req#18] [Mandatory]
│   │   ├── «feature» StockMonitoring
│   │   └── «feature» UsageTracking
│   ├── «feature» Restocking [Req#19] [Mandatory]
│   │   ├── «feature» AutomaticReordering
│   │   └── «feature» SupplierIntegration
│   └── «feature» InternalGoodsProvider [Mandatory]
│       └── «feature» ProductionSupply
│
└── «feature» WebInterface
    ├── «feature» CustomerInterface [Mandatory]
    │   ├── «feature» Ordering
    │   │   ├── «feature» MenuSelection [Req#7]
    │   │   ├── «feature» Customization [Req#8]
    │   │   │   ├── «feature» IngredientSelection
    │   │   │   ├── «feature» ShapeSelection
    │   │   │   └── «feature» SizeSelection
    │   │   └── «feature» QuantitySelection [Req#10]
    │   ├── «feature» Payment [Req#13] [Mandatory]
    │   │   ├── «feature» CreditCard
    │   │   ├── «feature» DigitalWallet
    │   │   └── «feature» OnlinePayment
    │   └── «feature» OrderTracking [Req#17]
    │       ├── «feature» StatusUpdates
    │       └── «feature» DeliveryTracking
    │
    ├── «feature» EmployeeInterface [Mandatory]
    │   ├── «feature» SystemManagement
    │   │   ├── «feature» ProductionControl
    │   │   └── «feature» MaintenanceMode
    │   ├── «feature» UserManagement
    │   └── «feature» ConfigurationManagement
    │
    └── «feature» Analytics [Optional]
        ├── «feature» ProductionMetrics
        ├── «feature» QualityMetrics
        └── «feature» BusinessIntelligence
```


