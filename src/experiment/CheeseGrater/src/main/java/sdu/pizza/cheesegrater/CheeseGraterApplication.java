package sdu.pizza.cheesegrater;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.kafka.annotation.EnableKafka;

@SpringBootApplication
@EnableKafka // Enable Kafka listener support
public class CheeseGraterApplication {
    public static void main(String[] args) {
        SpringApplication.run(CheeseGraterApplication.class, args);
    }
}
