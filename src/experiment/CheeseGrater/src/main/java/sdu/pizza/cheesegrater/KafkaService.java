package sdu.pizza.cheesegrater;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;

import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.atomic.AtomicBoolean;

@Service
public class KafkaService {

    private static final Logger log = LoggerFactory.getLogger(KafkaService.class);

    // Consuming from topic 'cheese-machine'
    private static final String CONSUME_TOPIC = "cheese-machine";

    // Producing to the next topic 'meat-machine'
    private static final String NEXT_TOPIC = "meat-machine";

    // Producing our "done" signal to the topic 'cheese-machine-done'
    private static final String DONE_TOPIC = "cheese-machine-done";

    // Signal topic from the next machine
    private static final String SIGNAL_TOPIC = "meat-machine-done";

    @Autowired
    private KafkaTemplate<String, Object> kafkaTemplate;

    // Queue to hold incoming pizzas
    private final BlockingQueue<PizzaOrderMessage> pizzaQueue = new LinkedBlockingQueue<>();

    // Signal to track if meat machine is ready
    private final AtomicBoolean isMeatMachineReady = new AtomicBoolean(true); // Start ready for first pizza

    // Background thread for processing
    public KafkaService() {
        Thread processingThread = new Thread(this::processPizzas);
        processingThread.setDaemon(true);
        processingThread.start();
    }

    /**
     * Listens to incoming pizzas from the previous machine (sauce-machine or dough-shaper)
     */
    @KafkaListener(topics = CONSUME_TOPIC, groupId = "cheese-grater-group")
    public void handlePizzaIncoming(PizzaOrderMessage pizza) {
        log.info("--> Pizza {} (Order: {}) received. Adding to queue.", pizza.getPizzaId(), pizza.getOrderId());
        try {
            pizzaQueue.put(pizza); // Blocks if queue is full (shouldn't happen in practice)
        } catch (InterruptedException e) {
            log.error("Interrupted while adding pizza to queue", e);
            Thread.currentThread().interrupt();
        }
    }

    /**
     * Listens to "done" signals from the next machine (meat-machine-done)
     */
    @KafkaListener(topics = SIGNAL_TOPIC, groupId = "cheese-grater-signal-group")
    public void handleMeatMachineReady(PizzaDoneMessage doneMessage) {
        log.info("<-- [Meat Machine Ready] signal received for Pizza {} (Order: {}). Unlocking processor.",
                doneMessage.getPizzaId(), doneMessage.getOrderId());
        isMeatMachineReady.set(true);
    }

    /**
     * Background processing loop
     */
    private void processPizzas() {
        log.info("Main Processor thread started. Waiting for pizzas...");

        while (true) {
            try {
                // Step 1: Wait for a pizza from the queue
                log.info("Main Processor waiting for a pizza...");
                PizzaOrderMessage pizza = pizzaQueue.take(); // Blocks until a pizza arrives

                // Step 2: For pizzas after the first one, wait for the meat machine signal
                if (pizza.getPizzaId() > 1) {
                    log.info("Main Processor waiting for Meat Machine to be ready (Pizza {})...", pizza.getPizzaId());
                    while (!isMeatMachineReady.getAndSet(false)) {
                        Thread.sleep(50); // Small delay to avoid busy-waiting
                    }
                } else {
                    log.info("First pizza in order - processing immediately without waiting.");
                    isMeatMachineReady.set(false); // Consume the initial "ready" state
                }

                // Step 3: Process the pizza
                log.info("Grating cheese for Pizza {} (Order: {})...", pizza.getPizzaId(), pizza.getOrderId());
                Thread.sleep(750); // 750ms processing time

                pizza.setMsgDesc("Cheese grated");
                log.info("...Finished Pizza {} (Order: {}).", pizza.getPizzaId(), pizza.getOrderId());

                // Step 4: Send updated pizza to the next machine
                kafkaTemplate.send(NEXT_TOPIC, pizza);

                // Step 5: Send "done" signal back to previous machine
                PizzaDoneMessage doneMessage = new PizzaDoneMessage();
                doneMessage.setPizzaId(pizza.getPizzaId());
                doneMessage.setOrderId(pizza.getOrderId());
                doneMessage.setDoneMsg(true);
                kafkaTemplate.send(DONE_TOPIC, doneMessage);

            } catch (InterruptedException e) {
                log.error("Processing thread interrupted", e);
                Thread.currentThread().interrupt();
                break;
            } catch (Exception e) {
                log.error("Error processing pizza", e);
            }
        }
    }
}