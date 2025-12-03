#!/bin/bash

# Create 10 orders of each size (1, 10, 50, 100 pizzas) in an interleaved pattern
for i in {1..10}; do
    npx tsx newOrders.ts 1 true
    npx tsx newOrders.ts 10 false
    npx tsx newOrders.ts 50 true
    npx tsx newOrders.ts 100 false
done