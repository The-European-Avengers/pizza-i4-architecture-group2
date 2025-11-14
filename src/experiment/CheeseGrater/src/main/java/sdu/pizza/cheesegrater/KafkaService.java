package sdu.pizza.cheesegrater;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;

@Service
public class KafkaService {

    private static final Logger log = LoggerFactory.getLogger(KafkaService.class);

    // Consuming from topic 'cheese-machine'
    //private static final String CONSUME_TOPIC = "cheese-machine";
    private static final String CONSUME_TOPIC = "cheese-machine";

    // Producing to the next topic 'meat-machine'
    //private static final String NEXT_TOPIC = "meat-machine";
    private static final String NEXT_TOPIC = "sauce-machine";

    // Producing our "done" signal to the topic 'cheese-machine-done'
    private static final String DONE_TOPIC = "cheese-machine-done";

    @Autowired
    private KafkaTemplate<String, Object> kafkaTemplate;

    @KafkaListener(topics = CONSUME_TOPIC, groupId = "cheese-grater-group")
    public void handlePizza(PizzaOrderMessage pizza) {
        log.info("🧀 Grating cheese for Pizza {} (Order: {})...", pizza.getPizzaId(), pizza.getOrderId());

        try {
            // --- Simulate work ---
            Thread.sleep(750); // 750ms processing time

            // Update the message description
            pizza.setMsgDesc("Cheese grated");
            log.info("...Finished Pizza {} (Order: {}).", pizza.getPizzaId(), pizza.getOrderId());

            // 1. Send updated PizzaOrderMessage to Meat Machine
            kafkaTemplate.send(NEXT_TOPIC, pizza);

            // 2. Send "PizzaDoneMessage" to our done topic
            PizzaDoneMessage doneMessage = new PizzaDoneMessage();
            doneMessage.setPizzaId(pizza.getPizzaId());
            doneMessage.setOrderId(pizza.getOrderId());
            doneMessage.setDoneMsg(true);

            kafkaTemplate.send(DONE_TOPIC, doneMessage);

        } catch (InterruptedException e) {
            log.error("Thread interrupted", e);
            Thread.currentThread().interrupt();
        } catch (Exception e) {
            log.error("Error processing pizza", e);
        }
    }
}