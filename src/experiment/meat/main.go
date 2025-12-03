package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/segmentio/kafka-go"
)

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

type DoneMessage struct {
	PizzaId int    `json:"pizzaId"`
	OrderId string `json:"orderId"`
	DoneMsg bool   `json:"doneMsg"`
}

// Restock request structures

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

type RestockDoneItem struct {
	ItemType        string `json:"itemType"`
	DeliveredAmount int    `json:"deliveredAmount"`
}

type RestockDoneMessage struct {
	Items []RestockDoneItem `json:"items"`
}

// track if the next machine is busy
var nextMachineBusy = false

const MAX_STOCK = 50

// Meat stock levels
var meatStock = map[string]int{
	"pepperoni":       40,
	"sausage":         40,
	"ham":             40,
	"grilled chicken": 40,
	"bacon":           40,
	"ground beef":     40,
}

var restockInProgress = false

func main() {
	consumeTopic := "meat-machine"
	consumeTopicDone := "vegetables-machine-done"
	produceTopicNext := "vegetables-machine"
	produceTopicDone := "meat-machine-done"

	// restock topics
	restockRequestTopic := "meat-machine-restock"
	restockDoneTopic := "meat-machine-restock-done"

	kafkaAddr := "kafka-experiment:29092"

	msgChan := make(chan Pizza)
	doneChan := make(chan DoneMessage)
	restockDoneChan := make(chan RestockDoneMessage)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// shutdown handler
	sigs := make(chan os.Signal, 1)
	signal.Notify(sigs, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		<-sigs
		fmt.Println("\n🛑 Shutdown requested...")
		cancel()
	}()

	// start consumers
	go consumePizza(ctx, kafkaAddr, consumeTopic, msgChan)
	go consumeDone(ctx, kafkaAddr, consumeTopicDone, doneChan)
	go consumeRestockDone(ctx, kafkaAddr, restockDoneTopic, restockDoneChan)

	// producers
	writerNext := newWriter(kafkaAddr, produceTopicNext)
	writerDone := newWriter(kafkaAddr, produceTopicDone)
	writerRestock := newWriter(kafkaAddr, restockRequestTopic)
	defer writerNext.Close()
	defer writerDone.Close()
	defer writerRestock.Close()

	fmt.Println("🎧 Meat machine ready")

	for {
		select {
		case <-ctx.Done():
			fmt.Println("✔ Clean shutdown")
			return

		case doneRestock := <-restockDoneChan:
			handleRestockDone(doneRestock)

		case pizza := <-msgChan:
			processPizza(ctx, pizza, writerNext, writerDone, writerRestock, doneChan, restockDoneChan)
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

// ------------------ Consumers --------------------

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
			return
		}
		var pizza Pizza
		if err := json.Unmarshal(m.Value, &pizza); err != nil {
			log.Println("⚠️ Bad JSON:", err)
			continue
		}
		out <- pizza
	}
}

func consumeDone(ctx context.Context, broker, topic string, out chan<- DoneMessage) {
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
			return
		}
		var done DoneMessage
		if err := json.Unmarshal(m.Value, &done); err != nil {
			log.Println("⚠️ Bad done JSON:", err)
			continue
		}
		nextMachineBusy = false
		fmt.Printf("✅ Next machine free (pizzaId=%d)\n", done.PizzaId)
		out <- done
	}
}

func consumeRestockDone(ctx context.Context, broker, topic string, out chan<- RestockDoneMessage) {
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
			return
		}
		var msg RestockDoneMessage
		if err := json.Unmarshal(m.Value, &msg); err != nil {
			log.Println("⚠️ Bad restock done JSON:", err)
			continue
		}
		out <- msg
	}
}

// ------------------ Restocking Logic --------------------

func handleRestockDone(msg RestockDoneMessage) {
	for _, item := range msg.Items {
		if _, ok := meatStock[item.ItemType]; ok {
			meatStock[item.ItemType] += item.DeliveredAmount
			if meatStock[item.ItemType] > MAX_STOCK {
				meatStock[item.ItemType] = MAX_STOCK
			}
			fmt.Printf("Restock received for %s: +%d (now %d)\n",
				item.ItemType, item.DeliveredAmount, meatStock[item.ItemType])
		}
	}
	restockInProgress = false
}

func buildRestockRequest() RestockRequest {
	items := []RestockItem{}

	for meat, stock := range meatStock {
		if stock <= 20 { // include all <=20%
			items = append(items, RestockItem{
				ItemType:        meat,
				CurrentStock:    stock,
				RequestedAmount: MAX_STOCK - stock,
			})
		}
	}

	return RestockRequest{
		MachineId:        "meat-machine",
		Items:            items,
		RequestTimestamp: time.Now().UnixMilli(),
	}
}

func checkRestockNeeded() bool {
	for _, stock := range meatStock {
		if stock <= 10 { // 10% threshold
			return true
		}
	}
	return false
}

// ------------------ Pizza Processing --------------------

func processPizza(
	ctx context.Context,
	pizza Pizza,
	writerNext, writerDone, writerRestock *kafka.Writer,
	doneChan <-chan DoneMessage,
	restockDoneChan <-chan RestockDoneMessage,
) {
	fmt.Printf("🍖 Start meat processing for pizza %d\n", pizza.PizzaId)

	// Trigger restock if needed
	if !restockInProgress && checkRestockNeeded() {
		req := buildRestockRequest()
		data, _ := json.Marshal(req)
		writerRestock.WriteMessages(ctx, kafka.Message{Value: data, Key: []byte(pizza.OrderId)})
		restockInProgress = true
		fmt.Println("Restock request sent")
	}

	// Determine required meats
	for _, meat := range pizza.Meat {
		// Wait if a meat is unavailable
		for meatStock[meat] <= 0 {
			if !restockInProgress {
				// Trigger restock if not already in progress
				req := buildRestockRequest()
				data, _ := json.Marshal(req)
				writerRestock.WriteMessages(ctx, kafka.Message{Value: data, Key: []byte(pizza.OrderId)})
				restockInProgress = true
				fmt.Printf("Meat '%s' out of stock & restock requested, waiting...\n", meat)
			} else {
				fmt.Printf("Meat '%s' out of stock, waiting for restock... The request was made before\n", meat)
			}

			// Wait with select to allow processing restock messages
			select {
			case doneRestock := <-restockDoneChan:
				handleRestockDone(doneRestock)
			case <-time.After(1 * time.Second):
				// Continue checking
			case <-ctx.Done():
				return
			}
		}

		// Use 1 unit per meat
		meatStock[meat] -= 1
		fmt.Printf("Used %s, remaining stock: %d\n", meat, meatStock[meat])
	}

	numberMeat := len(pizza.Meat)
	workTime := time.Duration(numberMeat*500) * time.Millisecond // 500ms per meat (0.5s)
	time.Sleep(workTime)
	fmt.Printf("🍖 Meat added to pizza %d\n", pizza.PizzaId)
	// Update message
	pizza.MsgDesc = fmt.Sprintf("Meat added to pizza %d", pizza.PizzaId)

	// Notify previous machine
	done := DoneMessage{
		PizzaId: pizza.PizzaId,
		OrderId: pizza.OrderId,
		DoneMsg: true,
	}
	sendJSONPizza(ctx, writerDone, pizza.PizzaId, done)
	fmt.Println("📤 Sent done message")

	// Wait for next machine
	if nextMachineBusy {
		fmt.Println("⏳ Waiting for vegetables machine to be free...")
		for nextMachineBusy {
			select {
			case <-doneChan:
			case <-ctx.Done():
				return
			default:
				time.Sleep(100 * time.Millisecond)
			}
		}
		fmt.Println("✅ Vegetables machine ready")
	} else {
		fmt.Println("✅ Vegetables machine free, sending immediately")
	}

	// Send pizza to next machine
	sendJSONPizza(ctx, writerNext, pizza.PizzaId, pizza)
	fmt.Printf("➡️ Sent pizza %d to vegetables machine\n", pizza.PizzaId)

	nextMachineBusy = true
}

// ------------------ Utility --------------------

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
