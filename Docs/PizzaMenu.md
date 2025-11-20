## 🍕 Pizza Menu Definition

This file defines the standardized data structure used for all pizza recipes processed by the automated I4 factory system.
    

### 1\. Data Structure Overview

All pizza recipes are defined as a JSON object within a master JSON array. Each object contains five key properties that specify the ingredients and the pizza's identity.

| Property | Type | Description | Example |
| :--- | :--- | :--- | :--- |
| name | String | The official name of the pizza. Used for display and order tracking. | `"Supreme Deluxe"` |
| sauce | String | The primary sauce base. Must correspond to an available sauce dispenser. | `"tomato"` |
| cheese| Array of Strings | A list of cheese types. Defines which cheese hoppers are activated. | `["mozzarella", "cheddar"]` |
| meat | Array of Strings | A list of meat toppings. Defines which meat dispensing units are activated. | `["pepperoni", "sausage"]` |
| veggies | Array of Strings | A list of vegetable/non-meat toppings. | `["mushroom", "onion"]` |


### 2\. JSON Recipe Example

The following example demonstrates the required structure for a single pizza entry:

```json
{
  "name": "Pepperoni Classic",
  "sauce": "tomato",
  "cheese": ["mozzarella"],
  "meat": ["pepperoni"],
  "veggies": []
}
```

### 3\. Complete Menu Sample (10 Pizzas)

This array represents the complete menu catalog currently loaded into the system for automated production.

```json
[
  {
    "name": "Margherita",
    "sauce": "tomato",
    "cheese": ["mozzarella"],
    "meat": [],
    "veggies": ["basil"]
  },
  {
    "name": "Pepperoni Classic",
    "sauce": "tomato",
    "cheese": ["mozzarella"],
    "meat": ["pepperoni"],
    "veggies": []
  },
  {
    "name": "Supreme Deluxe",
    "sauce": "tomato",
    "cheese": ["mozzarella", "cheddar"],
    "meat": ["pepperoni", "sausage", "ham"],
    "veggies": ["mushroom", "onion", "green pepper", "black olive"]
  },
  {
    "name": "BBQ Chicken Ranch",
    "sauce": "BBQ Sauce",
    "cheese": ["mozzarella", "smoked provolone"],
    "meat": ["grilled chicken"],
    "veggies": ["red onion"]
  },
  {
    "name": "Vegetarian Pesto",
    "sauce": "Pesto",
    "cheese": ["mozzarella", "feta"],
    "meat": [],
    "veggies": ["spinach", "sun-dried tomato", "artichoke heart"]
  },
  {
    "name": "Four Cheese (Quattro Formaggi)",
    "sauce": "Olive Oil",
    "cheese": ["mozzarella", "provolone", "parmesan", "gorgonzola"],
    "meat": [],
    "veggies": []
  },
  {
    "name": "Hawaiian Delight",
    "sauce": "tomato",
    "cheese": ["mozzarella"],
    "meat": ["ham", "bacon"],
    "veggies": ["pineapple"]
  },
  {
    "name": "Spicy Sriracha Beef",
    "sauce": "Sriracha-Tomato Blend",
    "cheese": ["mozzarella", "jalapeño jack"],
    "meat": ["ground beef"],
    "veggies": ["jalapeño", "red bell pepper"]
  },
  {
    "name": "Truffle Mushroom",
    "sauce": "White Garlic Cream",
    "cheese": ["mozzarella"],
    "meat": [],
    "veggies": ["mushroom", "truffle"]
  },
  {
    "name": "Breakfast Pizza",
    "sauce": "Hollandaise Sauce",
    "cheese": ["mozzarella", "cheddar"],
    "meat": ["sausage", "bacon"],
    "veggies": ["scrambled egg", "chives"]
  }
]
```


## 📋 I4 Pizza Factory Ingredient Inventory

### 1. Sauces (Base Layer)

These are the liquid bases applied to the dough.

* **tomato** 
* **BBQ Sauce**
* **Pesto**
* **Olive Oil**
* **Sriracha-Tomato Blend**
* **White Garlic Cream**
* **Hollandaise Sauce**

### 2. Cheeses

These cheeses will require dedicated hoppers or dispensing mechanisms.

* **mozzarella**
* **cheddar**
* **smoked provolone**
* **feta**
* **provolone**
* **parmesan**
* **gorgonzola**
* **jalapeño jack**

### 3. Meats

These proteins will be dispensed using automated units.

* **pepperoni**
* **sausage**
* **ham**
* **grilled chicken**
* **bacon**
* **ground beef**

### 4. Vegetables and Other Toppings

This diverse category includes all non-meat, non-cheese, and non-sauce items.

* **basil**
* **mushroom**
* **onion**
* **green pepper**
* **black olive**
* **red onion**
* **spinach**
* **truffle**
* **sun-dried tomato**
* **artichoke heart**
* **pineapple**
* **jalapeño**
* **red bell pepper**
* **scrambled egg**
* **chives**
