This file explains how to run the `newOrders.ts` script (generates a random order) using `npx tsx`.

**Script location**: `src/web/experiment/newOrders.ts`

**Requirements**:

- Node.js (recommended >= 16; 18+ preferred).
- `npm` (included with Node.js) or a compatible package manager.

**Quick steps**

1. Open a terminal and change to the experiment directory:

````powershell
```markdown
# Run the `newOrders.ts` script

This document explains how to run the `newOrders.ts` script, which generates a random pizza order and prints it as JSON.

Script path

- `src/web/experiment/newOrders.ts`

Requirements

- Node.js (recommended >= 16; 18+ preferred)
- `npm` (included with Node.js) or another package manager

Quick start (recommended)

1. Open a terminal and change to the experiment directory:

```powershell
cd src/web/experiment
````

2. Install dependencies (first time or after changes):

```powershell
npm install
```

3. Run the script with `npx` (no global install needed), with the amount of pizzas in the order(N):

```powershell
npx tsx newOrders.ts N
```

Run from project root

Instead of changing directories, you can run the script directly from the repository root:

```powershell
npx tsx src/web/experiment/newOrders.ts N
```

Notes and troubleshooting

- The project already lists `tsx` under `devDependencies`, so `npm install` enables `npx tsx ...`.
- If you get an error about missing packages (for example `uuid`), run `npm install` in the `src/web/experiment` directory.
- If PowerShell blocks execution or you encounter permission errors, try running the shell as Administrator or install `tsx` globally.

Example output

```json
{
  "OrderId": "...uuid...",
  "items": [{ "pizzaName": "Margherita", "quantity": 2 }],
  "createdAt": "2025-11-19T..."
}
```

Example response from the client gateway

```json
{
  "success": true,
  "message": "Order sent to Kafka topic: order-stack",
  "newOrder": {
    "OrderId": "uuid4",
    "items": [
      {
        "pizzaName": "Margherita",
        "quantity": 3
      },
      {
        "pizzaName": "Truffle Mushroom",
        "quantity": 1
      },
      {
        "pizzaName": "Four Cheese (Quattro Formaggi)",
        "quantity": 5
      }
    ],
    "createdAt": "2025-11-19T15:10:26.577Z"
  }
}
```
