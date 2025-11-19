#!/bin/bash

# Pizza Order Test Script using kafkacat/kcat
# Install: brew install kcat (macOS) or apt-get install kafkacat (Linux)

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
    echo ""
    echo "Please install one of them:"
    echo "  macOS:   brew install kcat"
    echo "  Linux:   apt-get install kafkacat"
    echo "  Or use the Python script instead: ./run_test_orders.sh"
    exit 1
fi

echo "✅ Using: $KAFKA_CMD"
echo ""

# Generate a UUID (works on macOS and Linux)
generate_uuid() {
    if command -v uuidgen &> /dev/null; then
        uuidgen | tr '[:upper:]' '[:lower:]'
    else
        # Fallback: generate a pseudo-UUID
        cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "$(date +%s)-$RANDOM-$RANDOM-$RANDOM-$RANDOM"
    fi
}

echo "📦 Sending test orders..."
echo ""

# Order 1: Mixed order
echo "📋 Order 1: Mixed Order"
ORDER_ID=$(generate_uuid)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Pepperoni Classic":2,"Four Cheese (Quattro Formaggi)":5,"Margherita":7},"isBaked":true}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID"
echo ""
sleep 1

# Order 2: Hawaiian party
echo "📋 Order 2: Hawaiian Delight Party"
ORDER_ID=$(generate_uuid)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Hawaiian Delight":10},"isBaked":true}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID"
echo ""
sleep 1

# Order 3: Vegetarian, not baked
echo "📋 Order 3: Vegetarian Selection (Not Baked)"
ORDER_ID=$(generate_uuid)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Margherita":3,"Vegetarian Pesto":2,"Truffle Mushroom":1},"isBaked":false}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID"
echo ""
sleep 1

# Order 4: Breakfast
echo "📋 Order 4: Breakfast Special"
ORDER_ID=$(generate_uuid)
ORDER_JSON='{"orderID":"'$ORDER_ID'","pizzas":{"Breakfast Pizza":5},"isBaked":true}'
echo "$ORDER_JSON" | $KAFKA_CMD -P -b $KAFKA_BROKER -t $TOPIC -k "$ORDER_ID"
echo "✅ Sent order: $ORDER_ID"
echo ""

echo "✅ All test orders sent!"
echo ""
echo "Check your order-processor logs:"
echo "  docker logs -f order-processor"