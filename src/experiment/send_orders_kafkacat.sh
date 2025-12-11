#!/bin/bash

# Pizza Order Test Script using kafkacat/kcat

KAFKA_BROKER="localhost:9092"
TOPIC="order-stack"

echo "🍕 Pizza Order Test Script (kafkacat version)"
echo "=============================================="

# Check if kcat or kafkacat is available
if command -v kcat &> /dev/null; then
    KAFKA_CMD="kcat"
elif command -v kafkacat &> /dev/null; then
    KAFKA_CMD="kafkacat"
else
    echo "❌ Neither kcat nor kafkacat is installed."
    exit 1
fi

echo "✅ Using: $KAFKA_CMD"
echo ""

# Generate a UUID
generate_uuid() {
    if command -v uuidgen &> /dev/null; then
        uuidgen | tr '[:upper:]' '[:lower:]'
    else
        cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "$(date +%s)-$RANDOM"
    fi
}

# Generate Unix Epoch Timestamp in Milliseconds (13 digits)
generate_timestamp() {
    # Try Python3 first (Works reliably on macOS and Linux)
    if command -v python3 &> /dev/null; then
        python3 -c 'import time; print(int(time.time() * 1000))'
    # Try Linux date with nanoseconds truncated to milliseconds
    elif date +%s%3N 2>&1 | grep -q '^[0-9]\+$'; then
        date +%s%3N
    # Fallback for macOS without Python (Seconds + 000)
    else
        echo "$(date +%s)000"
    fi
}

echo "📦 Sending test orders..."
echo ""

# Order 1: Mixed order
echo "📋 Order 1: Mixed Order"
ORDER_ID=$(generate_uuid)
TIMESTAMP=$(generate_timestamp)
# Note: timestamp is sent as a Number (no quotes)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Pepperoni Classic":2,"Four Cheese (Quattro Formaggi)":5,"Margherita":7},"isBaked":true,"startTimestamp":'$TIMESTAMP'}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID (TS: $TIMESTAMP)"
echo ""
sleep 1

# Order 2: Hawaiian party
echo "📋 Order 2: Hawaiian Delight Party"
ORDER_ID=$(generate_uuid)
TIMESTAMP=$(generate_timestamp)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Hawaiian Delight":10},"isBaked":true,"startTimestamp":'$TIMESTAMP'}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID (TS: $TIMESTAMP)"
echo ""
sleep 1

# Order 3: Vegetarian
echo "📋 Order 3: Vegetarian Selection (Not Baked)"
ORDER_ID=$(generate_uuid)
TIMESTAMP=$(generate_timestamp)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Margherita":3,"Vegetarian Pesto":2,"Truffle Mushroom":1},"isBaked":false,"startTimestamp":'$TIMESTAMP'}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID (TS: $TIMESTAMP)"
echo ""
sleep 1

# Order 4: Breakfast
echo "📋 Order 4: Breakfast Special"
ORDER_ID=$(generate_uuid)
TIMESTAMP=$(generate_timestamp)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Breakfast Pizza":5},"isBaked":true,"startTimestamp":'$TIMESTAMP'}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID (TS: $TIMESTAMP)"
echo ""

echo "✅ All test orders sent!"