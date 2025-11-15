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

// -------------------- GLOBAL STATE --------------------

var (
	ovenBusy    = false
	freezerBusy = false
	mu          sync.Mutex
)

func main() {
	consumeTopic := "vegetables-machine"
	consumeTopicOvenDone := "oven-machine-done"
	consumeTopicFreezerDone := "freezer-machine-done"

	produceTopicNextOven := "oven-machine"
	produceTopicNextFreezer := "freezer-machine"
	produceTopicDone := "vegetables-machine-done"

	kafkaAddr := "kafka:9092"

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

	// producers
	writerDone := newWriter(kafkaAddr, produceTopicDone)
	writerOven := newWriter(kafkaAddr, produceTopicNextOven)
	writerFreezer := newWriter(kafkaAddr, produceTopicNextFreezer)

	defer writerDone.Close()
	defer writerOven.Close()
	defer writerFreezer.Close()

	fmt.Println("🥬 Vegetables machine ready")

	for {
		select {
		case <-ctx.Done():
			fmt.Println("✔ Clean shutdown")
			return
		case pizza := <-msgChan:
			go processPizza(ctx, pizza, writerDone, writerOven, writerFreezer, ovenDoneChan, freezerDoneChan)
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

		// Update busy flags
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

func processPizza(
	ctx context.Context,
	pizza Pizza,
	writerDone *kafka.Writer,
	writerOven *kafka.Writer,
	writerFreezer *kafka.Writer,
	ovenDoneChan <-chan DoneMessage,
	freezerDoneChan <-chan DoneMessage,
) {
	fmt.Printf("🥬 Adding veggies to pizza %d\n", pizza.PizzaId)
	time.Sleep(1 * time.Second)
	pizza.MsgDesc = fmt.Sprintf("Veggies added to pizza %d", pizza.PizzaId)

	// 1️⃣ Send vegetables-machine-done
	doneMsg := DoneMessage{
		PizzaId: pizza.PizzaId,
		OrderId: pizza.OrderId,
		DoneMsg: true,
	}
	sendJSON(ctx, writerDone, pizza.PizzaId, doneMsg)
	fmt.Println("📤 Sent vegetables-machine-done")

	// 2️⃣ Wait for next machine depending on Baked flag
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

func waitForMachine(machine string, doneChan <-chan DoneMessage) {
	mu.Lock()
	var busy bool
	if machine == "oven" {
		busy = ovenBusy
	} else {
		busy = freezerBusy
	}
	mu.Unlock()

	// If busy, wait for done message
	if busy {
		fmt.Printf("⏳ Waiting for %s-machine to be free...\n", machine)
		<-doneChan
	}

	// Mark machine busy
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
