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

// ---- MESSAGE TYPES ----

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

// Packaging machine per-pizza done event
type PackagingDone struct {
    PizzaId int  `json:"pizzaId"`
    OrderId int  `json:"orderId"`
    DoneMsg bool `json:"doneMsg"`
}

// Final order done message → MUST include endTimestamp
type OrderDone struct {
    OrderId     int   `json:"orderId"`
    EndTimestamp int64 `json:"endTimestamp"`
}

// Track pizzas per order
var pizzasCompleted = make(map[int]int)

func main() {
    consumeTopic := "packaging-machine"
    produceTopicDone := "packaging-machine-done"
    produceOrderDone := "order-done"

    kafkaAddr := "kafka:29092"

    msgChan := make(chan Pizza)

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

    // producers
    writerDone := newWriter(kafkaAddr, produceTopicDone)
    writerOrderDone := newWriter(kafkaAddr, produceOrderDone)
    defer writerDone.Close()
    defer writerOrderDone.Close()

    fmt.Println("📦 Packaging machine ready")

    for {
        select {
        case <-ctx.Done():
            fmt.Println("✔ Clean shutdown")
            return

        case pizza := <-msgChan:
            processPizza(ctx, pizza, writerDone, writerOrderDone)
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

func processPizza(
    ctx context.Context,
    pizza Pizza,
    writerDone *kafka.Writer,
    writerOrderDone *kafka.Writer,
) {
    fmt.Printf("📦 Packaging pizza %d (order %d)\n", pizza.PizzaId, pizza.OrderId)
    time.Sleep(1 * time.Second)

    // Update message
    pizza.MsgDesc = fmt.Sprintf("Pizza %d has been packaged", pizza.PizzaId)

    // Send per-pizza done event
    doneMsg := PackagingDone{
        PizzaId: pizza.PizzaId,
        OrderId: pizza.OrderId,
        DoneMsg: true,
    }
    sendJSON(ctx, writerDone, pizza.PizzaId, doneMsg)
    fmt.Printf("📤 Sent packaging-done for pizza %d\n", pizza.PizzaId)

    // Count pizzas
    pizzasCompleted[pizza.OrderId]++

    // If all pizzas packaged → SEND order-done
    if pizzasCompleted[pizza.OrderId] == pizza.OrderSize {
        end := time.Now().UnixMilli()

        orderDone := OrderDone{
            OrderId:     pizza.OrderId,
            EndTimestamp: end,
        }

        sendJSON(ctx, writerOrderDone, pizza.OrderId, orderDone)
        fmt.Printf("🎉 Order %d completed → sent order-done\n", pizza.OrderId)

        delete(pizzasCompleted, pizza.OrderId)
    }
}

func sendJSON(ctx context.Context, writer *kafka.Writer, key int, value interface{}) {
    b, _ := json.Marshal(value)
    writer.WriteMessages(ctx, kafka.Message{
        Key:   []byte(fmt.Sprintf("%d", key)),
        Value: b,
    })
}
