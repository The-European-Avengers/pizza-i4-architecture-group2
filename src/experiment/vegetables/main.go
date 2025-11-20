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

// -------------------- MESSAGE TYPES --------------------

type Pizza struct {
	PizzaId        int      `json:"pizzaId"`
	OrderId        string      `json:"orderId"`
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
	PizzaId int  `json:"pizzaId"`
	OrderId string  `json:"orderId"`
	DoneMsg bool `json:"doneMsg"`
}

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

const MAX_STOCK = 100

// -------------------- GLOBAL STATE --------------------

var (
	ovenBusy    = false
	freezerBusy = false

	// Stock of all veggies
	veggieStock = map[string]int{
		"basil":            11,
		"mushroom":         11,
		"onion":            11,
		"green pepper":     11,
		"black olive":      11,
		"red onion":        11,
		"spinach":          11,
		"truffle":          11,
		"sun-dried tomato": 11,
		"artichoke heart":  11,
		"pineapple":        11,
		"jalapeño":         11,
		"red bell pepper":  11,
		"scrambled egg":    11,
		"chives":           11,
	}

	restockInProgress = false
)

// -------------------- MAIN --------------------

func main() {
	consumeTopic := "vegetables-machine"
	consumeTopicOvenDone := "oven-machine-done"
	consumeTopicFreezerDone := "freezer-machine-done"

	consumeRestockDoneTopic := "vegetables-machine-restock-done"
	produceRestockTopic := "vegetables-machine-restock"

	produceTopicNextOven := "oven-machine"
	produceTopicNextFreezer := "freezer-machine"
	produceTopicDone := "vegetables-machine-done"

	kafkaAddr := "kafka-experiment:29092"

	msgChan := make(chan Pizza)
	ovenDoneChan := make(chan DoneMessage, 1)
	freezerDoneChan := make(chan DoneMessage, 1)
	restockDoneChan := make(chan RestockDoneMessage) // Added RestockDoneMessage channel

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

	// consumers
	go consumePizza(ctx, kafkaAddr, consumeTopic, msgChan)
	go consumeDone(ctx, kafkaAddr, consumeTopicOvenDone, "oven", ovenDoneChan)          // Pass machine type
	go consumeDone(ctx, kafkaAddr, consumeTopicFreezerDone, "freezer", freezerDoneChan) // Pass machine type

	// restock done listener
	go consumeRestockDone(ctx, kafkaAddr, consumeRestockDoneTopic, restockDoneChan) // Use RestockDoneMessage channel

	// producers
	writerDone := newWriter(kafkaAddr, produceTopicDone)
	writerOven := newWriter(kafkaAddr, produceTopicNextOven)
	writerFreezer := newWriter(kafkaAddr, produceTopicNextFreezer)
	writerRestock := newWriter(kafkaAddr, produceRestockTopic)

	defer writerDone.Close()
	defer writerOven.Close()
	defer writerFreezer.Close()
	defer writerRestock.Close()

	fmt.Println("🥬 Vegetables machine ready")

	for {
		select {
		case <-ctx.Done():
			fmt.Println("✔ Clean shutdown")
			return

		case doneRestock := <-restockDoneChan: // Handle restock done
			handleRestockDone(doneRestock)

		case pizza := <-msgChan:
			// Removed 'go' keyword for sequential processing
			processPizza(ctx, pizza, writerDone, writerOven, writerFreezer, writerRestock, ovenDoneChan, freezerDoneChan)
		}
	}
}

// -------------------- KAFKA HELPERS --------------------

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
			log.Println("❌ Error reading pizza message:", err)
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

// Updated consumeDone to directly manage busy state
func consumeDone(ctx context.Context, broker, topic, machineType string, out chan<- DoneMessage) {
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
			log.Println("❌ Error reading done message:", err)
			continue
		}
		var done DoneMessage
		if err := json.Unmarshal(m.Value, &done); err != nil {
			log.Println("⚠️ Bad done JSON:", err)
			continue
		}

		if machineType == "oven" {
			ovenBusy = false
		} else if machineType == "freezer" {
			freezerBusy = false
		}
		fmt.Printf("✅ %s machine free (pizzaId=%d)\n", machineType, done.PizzaId)

		// Send to the channel for the blocking wait in processPizza
		out <- done
	}
}

// Updated to use RestockDoneMessage and handle restocking
func consumeRestockDone(ctx context.Context, broker, topic string, out chan<- RestockDoneMessage) {
	reader := kafka.NewReader(kafka.ReaderConfig{
		Brokers:     []string{broker},
		Topic:       topic,
		GroupID:     "vegetables-restock-done-group",
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

		out <- msg // Send to main loop
	}
}

// Added handler for restock done, similar to meat.go
func handleRestockDone(msg RestockDoneMessage) {
	for _, item := range msg.Items {
		if _, ok := veggieStock[item.ItemType]; ok {
			veggieStock[item.ItemType] += item.DeliveredAmount
			if veggieStock[item.ItemType] > MAX_STOCK {
				veggieStock[item.ItemType] = MAX_STOCK
			}
			fmt.Printf("Restock received for %s: +%d (now %d)\n",
				item.ItemType, item.DeliveredAmount, veggieStock[item.ItemType])
		}
	}
	restockInProgress = false
}

// -------------------- PROCESSING --------------------

func processPizza(
	ctx context.Context,
	pizza Pizza,
	writerDone *kafka.Writer,
	writerOven *kafka.Writer,
	writerFreezer *kafka.Writer,
	writerRestock *kafka.Writer,
	ovenDoneChan <-chan DoneMessage,
	freezerDoneChan <-chan DoneMessage,
) {
	fmt.Printf("🥬 Adding veggies to pizza %d\n", pizza.PizzaId)

	// Trigger restock if needed
	if !restockInProgress && checkRestockNeeded(pizza.Veggies) {
		req := buildRestockRequest(pizza.Veggies)
		data, _ := json.Marshal(req)
		writerRestock.WriteMessages(ctx, kafka.Message{Value: data, Key: []byte("vegetables-machine")})
		restockInProgress = true
		fmt.Println("Restock request sent")
	}

	// Wait for any veggie that is at zero
	waitForVeggies(ctx, pizza.Veggies)

	// Consume veggie stock
	for _, v := range pizza.Veggies {
		if veggieStock[v] > 0 {
			veggieStock[v]--
		}
		fmt.Printf("Used %s, remaining stock: %d\n", v, veggieStock[v])
	}

	numberVeggies := len(pizza.Veggies)
	workTime := time.Duration(numberVeggies*500) * time.Millisecond // 500ms per veggie (0.5s)
	time.Sleep(workTime)
	fmt.Printf("🥬 Veggies added to pizza %d\n", pizza.PizzaId)

	// Update message
	pizza.MsgDesc = fmt.Sprintf("Veggies added to pizza %d", pizza.PizzaId)

	// Send vegetables-machine-done
	doneMsg := DoneMessage{
		PizzaId: pizza.PizzaId,
		OrderId: pizza.OrderId,
		DoneMsg: true,
	}
	sendJSONPizza(ctx, writerDone, pizza.PizzaId, doneMsg)
	fmt.Println("📤 Sent vegetables-machine-done")

	// Route to correct machine
	if pizza.Baked {
		waitForMachine("oven", ovenDoneChan, ctx)
		sendJSONPizza(ctx, writerOven, pizza.PizzaId, pizza)
		fmt.Printf("➡️ Sent pizza %d to oven-machine\n", pizza.PizzaId)
		ovenBusy = true
	} else {
		waitForMachine("freezer", freezerDoneChan, ctx)
		sendJSONPizza(ctx, writerFreezer, pizza.PizzaId, pizza)
		fmt.Printf("➡️ Sent pizza %d to freezer-machine\n", pizza.PizzaId)
		freezerBusy = true
	}
}

func waitForVeggies(ctx context.Context, veggies []string) {
	for {
		needsWait := false
		for _, v := range veggies {
			if veggieStock[v] == 0 {
				needsWait = true
				fmt.Printf("Veggie '%s' out of stock, waiting...\n", v)
				break
			}
		}

		if !needsWait {
			return
		}

		select {
		case <-ctx.Done():
			return
		default:
			time.Sleep(200 * time.Millisecond)
		}
	}
}

func checkRestockNeeded(currentVeggies []string) bool {
	for v, stock := range veggieStock {
		// Use the more complex logic from the original file (but fixed to not use locks)
		threshold10 := 10
		threshold20 := 20

		if contains(currentVeggies, v) && stock <= threshold10 {
			return true
		} else if stock <= threshold20 {
			return true
		}
	}
	return false
}

func buildRestockRequest(currentVeggies []string) RestockRequest {
	items := []RestockItem{}
	threshold10 := 10
	threshold20 := 20

	for v, stock := range veggieStock {
		// Veggie required for current pizza and dangerously low (<= 10)
		if contains(currentVeggies, v) && stock <= threshold10 {
			items = append(items, RestockItem{
				ItemType:        v,
				CurrentStock:    stock,
				RequestedAmount: MAX_STOCK - stock,
			})
			// Any veggie low (<= 20)
		} else if stock <= threshold20 {
			items = append(items, RestockItem{
				ItemType:        v,
				CurrentStock:    stock,
				RequestedAmount: MAX_STOCK - stock,
			})
		}
	}

	return RestockRequest{
		MachineId:        "vegetables-machine",
		Items:            items,
		RequestTimestamp: time.Now().UnixMilli(),
	}
}

// Updated waitForMachine to use select and check busy state from global variables
func waitForMachine(machine string, doneChan <-chan DoneMessage, ctx context.Context) {
	var busy *bool
	if machine == "oven" {
		busy = &ovenBusy
	} else {
		busy = &freezerBusy
	}

	if *busy {
		fmt.Printf("⏳ Waiting for %s-machine to be free...\n", machine)

		// Block and wait for the done message from the consumer
		select {
		case <-doneChan:
			fmt.Printf("✅ %s-machine is now free\n", machine)
		case <-ctx.Done():
			return
		}
	} else {
		fmt.Printf("✅ %s-machine free, sending immediately\n", machine)
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


// -------------------- UTILS --------------------

func contains(list []string, item string) bool {
	for _, v := range list {
		if v == item {
			return true
		}
	}
	return false
}
