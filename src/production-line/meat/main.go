package main

import (
    "context"
    "encoding/json"
    "fmt"
    "log"
    "math/rand"
    "os"
    "os/signal"
    "syscall"
    "time"

    "github.com/segmentio/kafka-go"
)

type Order struct {
    Id int `json:"id"`
    Timestamp float64 `json:"timestamp"`
    Sauce  string   `json:"sauce"`
    Baked  bool     `json:"baked"`
    Cheese string   `json:"cheese"`
    Meat   []string `json:"meat"`
    Veggies []string `json:"veggies"`
}

type Result struct {
    Id int    `json:"id"`
    Timestamp  float64 `json:"timestamp"`
    Sauce  string   `json:"sauce"`
    Baked  bool     `json:"baked"`
    Cheese string   `json:"cheese"`
    Meat   []string `json:"meat"`
    Veggies []string `json:"veggies"`
}

func main() {
    consumeTopic := "meat-machine"
    produceTopic := "vegetables-machine"
    kafkaAddr := "127.0.0.1:9092"

    ctx, cancel := context.WithCancel(context.Background())
    defer cancel()

    // Channel for messages coming from Kafka
    msgChan := make(chan Order)

    // Signal handling for graceful shutdown
    sigs := make(chan os.Signal, 1)
    signal.Notify(sigs, syscall.SIGINT, syscall.SIGTERM)

    go func() {
        <-sigs
        fmt.Println("\n🛑 Stopping service...")
        cancel()
    }()

    // Start Kafka consumer goroutine
    go consumeKafka(ctx, kafkaAddr, consumeTopic, msgChan)

    // Kafka producer
    writer := &kafka.Writer{
        Addr:     kafka.TCP(kafkaAddr),
        Topic:    produceTopic,
        Balancer: &kafka.LeastBytes{},
    }
    defer writer.Close()

    fmt.Printf("🎧 Listening on '%s' and producing to '%s'...\n", consumeTopic, produceTopic)

    // Main loop: process messages received through the channel
    for {
        select {
        case <-ctx.Done():
            fmt.Println("✅ Service stopped cleanly.")
            return
        case msg := <-msgChan:
            processAndProduce(ctx, writer, msg)
        }
    }
}

func consumeKafka(ctx context.Context, brokers, topic string, out chan<- Order) {
    reader := kafka.NewReader(kafka.ReaderConfig{
        Brokers:   []string{brokers},
        Topic:     topic,
        GroupID:   fmt.Sprintf("meat-worker-%d", time.Now().UnixNano()),
        MinBytes:  1e3,  // 1KB
        MaxBytes:  10e6, // 10MB
		StartOffset: kafka.LastOffset, // Start at latest message
    })
    defer reader.Close()

    for {
        m, err := reader.ReadMessage(ctx)
        if err != nil {
            if ctx.Err() != nil {
                return // Context cancelled
            }
            log.Printf("❌ Error reading message: %v", err)
            continue
        }

        var order Order
        if err := json.Unmarshal(m.Value, &order); err != nil {
            log.Printf("⚠️ Failed to unmarshal: %v", err)
            continue
        }

        log.Printf("📥 Received: %+v", order)
        out <- order
    }
}

func processAndProduce(ctx context.Context, writer *kafka.Writer, order Order) {
    fmt.Printf("🍖 Preparing meat for order %d...\n", order.Id)
    time.Sleep(time.Duration(rand.Intn(1000)+500) * time.Millisecond)
    fmt.Printf("🍖 Meat added to pizza for order %d.\n", order.Id)

    result := Result{
        Id: order.Id,
        Timestamp:  float64(time.Now().UnixNano()) / 1e9,
    }
    result.Sauce = "tomato"
    result.Baked = false
    result.Cheese = "mozzarella"
    result.Meat = []string{"pepperoni", "bacon"}
    result.Veggies = []string{"onion", "mushroom"}

    data, err := json.Marshal(result)
    if err != nil {
        log.Printf("⚠️ Failed to marshal result: %v", err)
        return
    }    

    err = writer.WriteMessages(ctx, kafka.Message{
		Key: []byte(fmt.Sprintf("%d", order.Id)),
        Value: data,
    })
    if err != nil {
        log.Printf("❌ Failed to produce message: %v", err)
        return
    }

    log.Printf("✅ Produced finished message for order %d", order.Id)
}
