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

// -------------------- MESSAGE TYPES --------------------

type Pizza struct {
	PizzaId        int      `json:"pizzaId"`
	OrderId        int      `json:"orderId"`
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
	OrderId int  `json:"orderId"`
	DoneMsg bool `json:"doneMsg"`
}

type RestockItem struct {
	ItemType       string `json:"itemType"`
	CurrentStock   int    `json:"currentStock"`
	RequestedAmount int   `json:"requestedAmount"`
}

type RestockRequest struct {
	MachineId        string        `json:"machineId"`
	Items            []RestockItem `json:"items"`
	RequestTimestamp int64         `json:"requestTimestamp"`
}

// -------------------- GLOBAL STATE --------------------

var (
	ovenBusy    = false
	freezerBusy = false
	mu          sync.Mutex

	// Stock of all veggies
	veggieStock = map[string]int{
		"basil":             100,
		"mushroom":          100,
		"onion":             100,
		"green pepper":      100,
		"black olive":       100,
		"red onion":         100,
		"spinach":           100,
		"truffle":           100,
		"sun-dried tomato":  100,
		"artichoke heart":   100,
		"pineapple":         100,
		"jalapeño":          100,
		"red bell pepper":   100,
		"scrambled egg":     100,
		"chives":            100,
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
	go consumeDone(ctx, kafkaAddr, consumeTopicOvenDone, ovenDoneChan)
	go consumeDone(ctx, kafkaAddr, consumeTopicFreezerDone, freezerDoneChan)

	// restock done listener
	go consumeRestockDone(ctx, kafkaAddr, consumeRestockDoneTopic)

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
		case pizza := <-msgChan:
			go processPizza(ctx, pizza, writerDone, writerOven, writerFreezer, writerRestock, ovenDoneChan, freezerDoneChan)
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

		mu.Lock()
		if topic == "oven-machine-done" {
			ovenBusy = false
		} else if topic == "freezer-machine-done" {
			freezerBusy = false
		}
		mu.Unlock()

		out <- done
	}
}

func consumeRestockDone(ctx context.Context, broker, topic string) {
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

		var payload struct {
			Items []RestockItem `json:"items"`
		}
		if err := json.Unmarshal(m.Value, &payload); err != nil {
			log.Println("⚠️ Bad restock-done JSON:", err)
			continue
		}

		mu.Lock()
		for _, item := range payload.Items {
			veggieStock[item.ItemType] += item.RequestedAmount
		}
		restockInProgress = false
		mu.Unlock()

		fmt.Println("🌱 Veggies restocked")
	}
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

	// Check stock and request restock if needed
	checkVeggieStock(ctx, pizza.Veggies, writerRestock)

	// Wait for any veggie that is at zero
	waitForVeggies(ctx, pizza.Veggies)

	// Consume veggie stock
	mu.Lock()
	for _, v := range pizza.Veggies {
		if veggieStock[v] > 0 {
			veggieStock[v]--
		}
	}
	mu.Unlock()

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
	sendJSON(ctx, writerDone, pizza.PizzaId, doneMsg)
	fmt.Println("📤 Sent vegetables-machine-done")

	// Route to correct machine
	if pizza.Baked {
		waitForMachine("oven", ovenDoneChan)
		sendJSON(ctx, writerOven, pizza.PizzaId, pizza)
		fmt.Printf("➡️ Sent pizza %d to oven-machine\n", pizza.PizzaId)
	} else {
		waitForMachine("freezer", freezerDoneChan)
		sendJSON(ctx, writerFreezer, pizza.PizzaId, pizza)
		fmt.Printf("➡️ Sent pizza %d to freezer-machine\n", pizza.PizzaId)
	}
}

func waitForVeggies(ctx context.Context, veggies []string) {
	for {
		mu.Lock()
		needsWait := false
		for _, v := range veggies {
			if veggieStock[v] == 0 {
				needsWait = true
				break
			}
		}
		mu.Unlock()

		if !needsWait {
			return
		}

		time.Sleep(200 * time.Millisecond)
	}
}

func checkVeggieStock(ctx context.Context, veggies []string, writer *kafka.Writer) {
	mu.Lock()
	defer mu.Unlock()

	if restockInProgress {
		return
	}

	low := []RestockItem{}
	for v, stock := range veggieStock {

		threshold10 := 10
		threshold20 := 20

		if contains(veggies, v) && stock <= threshold10 {
			low = append(low, RestockItem{
				ItemType:       v,
				CurrentStock:   stock,
				RequestedAmount: 100 - stock,
			})
		} else if stock <= threshold20 {
			low = append(low, RestockItem{
				ItemType:       v,
				CurrentStock:   stock,
				RequestedAmount: 100 - stock,
			})
		}
	}

	if len(low) == 0 {
		return
	}

	restockInProgress = true

	req := RestockRequest{
		MachineId:        "vegetables-machine",
		Items:            low,
		RequestTimestamp: time.Now().UnixMilli(),
	}

	b, _ := json.Marshal(req)
	writer.WriteMessages(ctx, kafka.Message{
		Key:   []byte("restock"),
		Value: b,
	})

	fmt.Println("📦 Requested veggie restock")
}

func waitForMachine(machine string, doneChan <-chan DoneMessage) {
	mu.Lock()
	var busy bool
	if machine == "oven" {
		busy = ovenBusy
	} else {
		busy = freezerBusy
	}
	mu.Unlock()

	if busy {
		fmt.Printf("⏳ Waiting for %s-machine to be free...\n", machine)
		<-doneChan
	}

	mu.Lock()
	if machine == "oven" {
		ovenBusy = true
	} else {
		freezerBusy = true
	}
	mu.Unlock()
	fmt.Printf("✅ %s-machine is now busy\n", machine)
}

func sendJSON(ctx context.Context, writer *kafka.Writer, key int, value interface{}) {
	b, err := json.Marshal(value)
	if err != nil {
		log.Println("❌ Failed to marshal JSON:", err)
		return
	}

	err = writer.WriteMessages(ctx, kafka.Message{
		Key:   []byte(fmt.Sprintf("%d", key)),
		Value: b,
	})
	if err != nil {
		log.Println("❌ Failed to write message:", err)
	}
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
