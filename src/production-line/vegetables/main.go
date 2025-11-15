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
	OrderId        int      `json:"orderId"`
	OrderSize      int      `json:"orderSize"`
	StartTimestamp int64    `json:"startTimestamp"`
	EndTimestamp   *int64   `json:"endTimestamp"`
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

// --------------------------------------------------------

func main() {
	consumeTopic := "vegetables-machine"
	consumeTopicOvenDone := "oven-machine-done"
	consumeTopicFreezerDone := "freezer-machine-done"

	produceTopicNextOven := "oven-machine"
	produceTopicNextFreezer := "freezer-machine"
	produceTopicDone := "vegetables-machine-done"

	kafkaAddr := "kafka:29092"

	msgChan := make(chan Pizza)
	ovenDoneChan := make(chan DoneMessage)
	freezerDoneChan := make(chan DoneMessage)

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
			processPizza(ctx, pizza, writerDone, writerOven, writerFreezer, ovenDoneChan, freezerDoneChan)
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

	// 2️⃣ If pizza is baked → next step is OVEN
	if pizza.Baked {
		fmt.Println("🔥 Pizza needs baking → waiting for oven-machine to be free...")
		<-ovenDoneChan
		fmt.Println("🔥 Oven is free → sending pizza")

		sendJSON(ctx, writerOven, pizza.PizzaId, pizza)
		fmt.Printf("➡️ Sent pizza %d to oven-machine\n", pizza.PizzaId)
		return
	}

	// 3️⃣ If pizza is NOT baked → goes to FREEZER
	fmt.Println("❄️ Pizza does NOT need baking → waiting for freezer-machine to be free...")
	<-freezerDoneChan
	fmt.Println("❄️ Freezer is free → sending pizza")

	sendJSON(ctx, writerFreezer, pizza.PizzaId, pizza)
	fmt.Printf("➡️ Sent pizza %d to freezer-machine\n", pizza.PizzaId)
}

func sendJSON(ctx context.Context, writer *kafka.Writer, key int, value interface{}) {
	b, _ := json.Marshal(value)
	writer.WriteMessages(ctx, kafka.Message{
		Key:   []byte(fmt.Sprintf("%d", key)),
		Value: b,
	})
}
