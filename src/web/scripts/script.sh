#!/bin/bash

for i in {1..10}; do
    npx tsx newOrders.ts 1 true
done
for i in {1..10}; do
    npx tsx newOrders.ts 10 false
done

for i in {1..10}; do
    npx tsx newOrders.ts 50 true
done

for i in {1..10}; do
    npx tsx newOrders.ts 100 false
done