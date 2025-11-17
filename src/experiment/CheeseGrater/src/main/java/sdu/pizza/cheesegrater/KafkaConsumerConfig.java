package sdu.pizza.cheesegrater;

import org.apache.kafka.clients.consumer.ConsumerConfig;
import org.apache.kafka.common.serialization.StringDeserializer;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.kafka.annotation.EnableKafka;
import org.springframework.kafka.config.ConcurrentKafkaListenerContainerFactory;
import org.springframework.kafka.core.ConsumerFactory;
import org.springframework.kafka.core.DefaultKafkaConsumerFactory;
import org.springframework.kafka.core.KafkaProperties;
import org.springframework.kafka.support.serializer.JsonDeserializer;

import java.util.Map;

@EnableKafka
@Configuration
public class KafkaConsumerConfig {

    @Autowired
    private KafkaProperties kafkaProperties; // Gets all settings from application.properties

    /**
     * Creates a dedicated ConsumerFactory for PizzaDoneMessage.
     * It uses a JsonDeserializer specifically configured for that class.
     */
    @Bean
    public ConsumerFactory<String, PizzaDoneMessage> pizzaDoneConsumerFactory() {
        // Get the base properties from application.properties
        Map<String, Object> props = kafkaProperties.buildConsumerProperties(null);

        // Configure the JsonDeserializer specifically for PizzaDoneMessage
        JsonDeserializer<PizzaDoneMessage> deserializer = new JsonDeserializer<>(PizzaDoneMessage.class);
        deserializer.setTrustedPackages("*"); // Trust all packages
        deserializer.setUseTypeHeaders(false); // Don't look for type headers

        return new DefaultKafkaConsumerFactory<>(props, new StringDeserializer(), deserializer);
    }

    /**
     * Creates a dedicated ListenerContainerFactory for our PizzaDoneMessage consumer.
     */
    @Bean
    public ConcurrentKafkaListenerContainerFactory<String, PizzaDoneMessage> pizzaDoneContainerFactory() {
        ConcurrentKafkaListenerContainerFactory<String, PizzaDoneMessage> factory =
                new ConcurrentKafkaListenerContainerFactory<>();
        factory.setConsumerFactory(pizzaDoneConsumerFactory());
        return factory;
    }
}