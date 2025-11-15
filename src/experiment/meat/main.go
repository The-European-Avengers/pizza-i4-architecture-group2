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

// track if the next machine is busy
var nextMachineBusy = false

func main() {
	consumeTopic := "meat-machine"
	consumeTopicDone := "vegetables-machine-done"
	produceTopicNext := "vegetables-machine"
	produceTopicDone := "meat-machine-done"

	kafkaAddr := "kafka-experiment:29092"

	msgChan := make(chan Pizza)
	doneChan := make(chan DoneMessage)

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

	// kafka producers
	writerNext := newWriter(kafkaAddr, produceTopicNext)
	writerDone := newWriter(kafkaAddr, produceTopicDone)
	defer writerNext.Close()
	defer writerDone.Close()

	fmt.Println("🎧 Meat machine ready")

	for {
		select {
		case <-ctx.Done():
			fmt.Println("✔ Clean shutdown")
			return
		case pizza := <-msgChan:
			processPizza(ctx, pizza, writerNext, writerDone, doneChan)
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
		// mark next machine as free
		nextMachineBusy = false
		fmt.Printf("✅ Next machine free (pizzaId=%d)\n", done.PizzaId)
		out <- done
	}
}

func processPizza(ctx context.Context, pizza Pizza, writerNext, writerDone *kafka.Writer, doneChan <-chan DoneMessage) {
	fmt.Printf("🍖 Start meat processing for pizza %d\n", pizza.PizzaId)
	time.Sleep(1 * time.Second)

	pizza.MsgDesc = fmt.Sprintf("Meat added to pizza %d", pizza.PizzaId)

	// Send done event for this machine
	done := DoneMessage{
		PizzaId: pizza.PizzaId,
		OrderId: pizza.OrderId,
		DoneMsg: true,
	}
	sendJSON(ctx, writerDone, pizza.PizzaId, done)
	fmt.Println("📤 Sent done message")

	// Wait for next machine only if busy
	if nextMachineBusy {
		fmt.Println("⏳ Waiting for vegetables machine to be free...")
		for nextMachineBusy {
			select {
			case <-doneChan:
				// next machine is free now
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
	sendJSON(ctx, writerNext, pizza.PizzaId, pizza)
	fmt.Printf("➡️ Sent pizza %d to vegetables machine\n", pizza.PizzaId)

	// Mark next machine as busy
	nextMachineBusy = true
}

func sendJSON(ctx context.Context, writer *kafka.Writer, key int, value interface{}) {
	b, _ := json.Marshal(value)
	writer.WriteMessages(ctx, kafka.Message{
		Key:   []byte(fmt.Sprintf("%d", key)),
		Value: b,
	})
}
