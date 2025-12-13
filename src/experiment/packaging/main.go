package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"

	"github.com/segmentio/kafka-go"
)

// ---- MESSAGE TYPES ----

type Pizza struct {
	PizzaId        int      `json:"pizzaId"`
	OrderId        string   `json:"orderId"`
	OrderSize      int      `json:"orderSize"`
	StartTimestamp float64  `json:"startTimestamp"`
	EndTimestamp   *float64 `json:"endTimestamp"`
	MsgDesc        string   `json:"msgDesc"`
	Sauce          string   `json:"sauce"`
	Baked          bool     `json:"baked"`
	Cheese         []string `json:"cheese"`
	Meat           []string `json:"meat"`
	Veggies        []string `json:"veggies"`
}

// Packaging machine per-pizza done event
type PackagingDone struct {
	PizzaId int    `json:"pizzaId"`
	OrderId string `json:"orderId"`
	DoneMsg bool   `json:"doneMsg"`
}

// Final order done message → MUST include endTimestamp
type OrderDone struct {
	OrderId      string  `json:"orderId"`
	EndTimestamp float64 `json:"endTimestamp"`
	OrderSize    int     `json:"orderSize`
}

type PizzaDone struct {
	OrderId      string   `json:"orderId"`
	OrderSize    int      `json:"orderSize"`
	PizzaId      int      `json:"pizzaId"`
	EndTimestamp float64  `json:"endTimestamp"`
	Sauce        string   `json:"sauce"`
	Cheese       []string `json:"cheese"`
	Meat         []string `json:"meat"`
	Veggies      []string `json:"veggies"`
	Baked        bool     `json:"baked"`
}

// Restock messages
type RestockItem struct {
	ItemType        string `json:"itemType"`
	CurrentStock    int    `json:"currentStock"`
	RequestedAmount int    `json:"requestedAmount"`
}

type RestockRequest struct {
	MachineId        string        `json:"machineId"`
	Items            []RestockItem `json:"items"`
	RequestTimestamp int64         `json:"requestTimestamp"`
}

// ADDED: Type for restock response, using delivered amount
type RestockDoneItem struct {
	ItemType        string `json:"itemType"`
	DeliveredAmount int    `json:"deliveredAmount"` 
}

type RestockDoneMessage struct {
	Items []RestockDoneItem `json:"items"`
}

const MAX_STOCK = 100 

// Track pizzas per order safely
var (
	pizzasCompleted = make(map[string]int)
	boxStock        = map[string]int{
		"box": 80,
	}
	mu                sync.Mutex
	restockInProgress = false
)

func main() {
	consumeTopic := "packaging-machine"
	produceTopicDone := "packaging-machine-done"
	produceOrderDone := "order-done"
	producePizzaDone := "pizza-done"
	produceRestock := "packaging-machine-restock"
	consumeRestockDoneTopic := "packaging-machine-restock-done"

	kafkaAddr := "kafka-experiment:29092"
	msgChan := make(chan Pizza)
	restockDoneChan := make(chan RestockDoneMessage) // Channel to pass restock data to a handler

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// graceful shutdown
	sigs := make(chan os.Signal, 1)
	signal.Notify(sigs, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		<-sigs
		fmt.Println("\n🛑 Shutdown requested...")
		cancel()
	}()

	// consumer
	go consumePizza(ctx, kafkaAddr, consumeTopic, msgChan)
	go consumeRestockDone(ctx, kafkaAddr, consumeRestockDoneTopic, restockDoneChan) // Pass restockDoneChan

	// producers
	writerDone := newWriter(kafkaAddr, produceTopicDone)
	writerOrderDone := newWriter(kafkaAddr, produceOrderDone)
	writerPizzaDone := newWriter(kafkaAddr, producePizzaDone)
	writerRestock := newWriter(kafkaAddr, produceRestock)
	defer writerDone.Close()
	defer writerOrderDone.Close()
	defer writerPizzaDone.Close()
	defer writerRestock.Close()

	fmt.Println("📦 Packaging machine ready")

	for {
		select {
		case <-ctx.Done():
			fmt.Println("✔ Clean shutdown")
			return

		case doneRestock := <-restockDoneChan: // Handle restock done in the main loop
			handleRestockDone(doneRestock)

		case pizza := <-msgChan:
			go processPizza(ctx, pizza, writerDone, writerOrderDone, writerPizzaDone, writerRestock)
		}
	}
}

func newWriter(addr, topic string) *kafka.Writer {
	return &kafka.Writer{
		Addr:     kafka.TCP(addr),
		Topic:    topic,
		Balancer: &kafka.LeastBytes{},
	}
}

func consumePizza(ctx context.Context, broker, topic string, out chan<- Pizza) {
	reader := kafka.NewReader(kafka.ReaderConfig{
		Brokers:     []string{broker},
		Topic:       topic,
		GroupID:     fmt.Sprintf("%s-group", topic),
		StartOffset: kafka.LastOffset,
	})
	defer reader.Close()

	for {
		m, err := reader.ReadMessage(ctx)
		if err != nil {
			if ctx.Err() != nil {
				return
			}
			log.Println("❌ Error reading message:", err)
			continue
		}

		var pizza Pizza
		if err := json.Unmarshal(m.Value, &pizza); err != nil {
			log.Println("⚠️ Bad JSON:", err)
			continue
		}
		out <- pizza
	}
}

func consumeRestockDone(ctx context.Context, broker, topic string, out chan<- RestockDoneMessage) {
	reader := kafka.NewReader(kafka.ReaderConfig{
		Brokers:     []string{broker},
		Topic:       topic,
		GroupID:     "packaging-restock-done-group",
		StartOffset: kafka.LastOffset,
	})
	defer reader.Close()

	for {
		m, err := reader.ReadMessage(ctx)
		if err != nil {
			return
		}
		var msg RestockDoneMessage
		if err := json.Unmarshal(m.Value, &msg); err != nil {
			log.Println("⚠️ Bad restock-done JSON:", err)
			continue
		}

		out <- msg // Send to main loop for thread-safe processing
	}
}

func handleRestockDone(msg RestockDoneMessage) {
	mu.Lock()
	defer mu.Unlock()

	for _, item := range msg.Items {
		if _, ok := boxStock[item.ItemType]; ok {
			boxStock[item.ItemType] += item.DeliveredAmount
			if boxStock[item.ItemType] > MAX_STOCK {
				boxStock[item.ItemType] = MAX_STOCK
			}
			fmt.Printf("Restock received for %s: +%d (now %d)\n",
				item.ItemType, item.DeliveredAmount, boxStock[item.ItemType])
		}
	}
	restockInProgress = false
	fmt.Println("📦 Boxes restocked")
}

func processPizza(
	ctx context.Context,
	pizza Pizza,
	writerDone *kafka.Writer,
	writerOrderDone *kafka.Writer,
	writerPizzaDone *kafka.Writer,
	writerRestock *kafka.Writer,
) {
	boxType := "box" 

	// Check stock & request restock if low
	checkBoxStock(ctx, boxType, writerRestock, pizza.OrderId)

	// Wait if no boxes
	waitForBoxes(ctx, boxType)

	// Consume box
	mu.Lock()
	boxStock[boxType]--
	mu.Unlock()

	fmt.Printf("📦 Packaging pizza %d (order %d)\n", pizza.PizzaId, pizza.OrderId)
	time.Sleep(1 * time.Second) // simulate work
	pizza.MsgDesc = fmt.Sprintf("Pizza %d has been packaged", pizza.PizzaId)

	// Send per-pizza done event
	doneMsg := PackagingDone{
		PizzaId: pizza.PizzaId,
		OrderId: pizza.OrderId,
		DoneMsg: true,
	}
	sendJSONPizza(ctx, writerDone, pizza.PizzaId, doneMsg)
	fmt.Printf("📤 Sent packaging-done for pizza %d\n", pizza.PizzaId)

	end := time.Now().UnixMilli()
	//Send to pizza-done topic
	pizzaDoneMsg := PizzaDone{
		PizzaId:      pizza.PizzaId,
		OrderId:      pizza.OrderId,
		OrderSize:    pizza.OrderSize,
		EndTimestamp: float64(end),
		Sauce:        pizza.Sauce,
		Cheese:       pizza.Cheese,
		Meat:         pizza.Meat,
		Veggies:      pizza.Veggies,
		Baked:        pizza.Baked,
	}
	sendJSONPizza(ctx, writerPizzaDone, pizza.PizzaId, pizzaDoneMsg)

	// Thread-safe count update
	mu.Lock()
	pizzasCompleted[pizza.OrderId]++
	completed := pizzasCompleted[pizza.OrderId]
	mu.Unlock()

	// If all pizzas packaged → send order-done
	if completed == pizza.OrderSize {
		orderDone := OrderDone{
			OrderId:      pizza.OrderId,
			EndTimestamp: float64(end),
			OrderSize:    pizza.OrderSize,
		}
		sendJSONOrder(ctx, writerOrderDone, pizza.OrderId, orderDone)
		fmt.Printf("🎉 Order %d completed → sent order-done\n", pizza.OrderId)

		mu.Lock()
		delete(pizzasCompleted, pizza.OrderId)
		mu.Unlock()
	}
}

func checkBoxStock(ctx context.Context, boxType string, writer *kafka.Writer, orderId string) {
	mu.Lock()
	defer mu.Unlock()

	if restockInProgress {
		return
	}
	// Assuming box stock threshold is 10 boxes (10% of 100 max stock)
	if boxStock[boxType] <= 10 {
		req := RestockRequest{
			MachineId: "packaging-machine",
			Items: []RestockItem{
				{
					ItemType:        boxType,
					CurrentStock:    boxStock[boxType],
					RequestedAmount: MAX_STOCK - boxStock[boxType], // Request up to MAX_STOCK
				},
			},
			RequestTimestamp: time.Now().UnixMilli(),
		}
		b, _ := json.Marshal(req)
		writer.WriteMessages(ctx, kafka.Message{
			Key:   []byte(orderId),
			Value: b,
		})
		restockInProgress = true
		fmt.Println("📦 Requested box restock")
	}
}

func waitForBoxes(ctx context.Context, boxType string) {
	for {
		mu.Lock()
		if boxStock[boxType] > 0 {
			mu.Unlock()
			return
		}
		mu.Unlock()
		time.Sleep(200 * time.Millisecond)
	}
}

func sendJSONOrder(ctx context.Context, writer *kafka.Writer, key string, value interface{}) {
	b, err := json.Marshal(value)
	if err != nil {
		log.Println("❌ Failed to marshal JSON:", err)
		return
	}
	writer.WriteMessages(ctx, kafka.Message{
		Key:   []byte(key),
		Value: b,
	})
}
func sendJSONPizza(ctx context.Context, writer *kafka.Writer, key int, value interface{}) {
	b, err := json.Marshal(value)
	if err != nil {
		log.Println("❌ Failed to marshal JSON:", err)
		return
	}
	writer.WriteMessages(ctx, kafka.Message{
		Key:   []byte(fmt.Sprintf("%d", key)),
		Value: b,
	})
}
