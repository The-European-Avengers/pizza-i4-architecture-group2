package sdu.pizza.cheesegrater;


import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data; // Using Lombok for boilerplate code
import java.util.List;

@Data // Creates getters, setters, toString, etc.
public class PizzaOrderMessage {
    @JsonProperty("pizzaId")
    private int pizzaId;

    @JsonProperty("orderId")
    private int orderId;

    @JsonProperty("orderSize")
    private int orderSize;

    @JsonProperty("startTimestamp")
    private Long startTimestamp;

    @JsonProperty("endTimestamp")
    private Long endTimestamp;

    @JsonProperty("msgDesc")
    private String msgDesc;

    @JsonProperty("sauce")
    private String sauce;

    @JsonProperty("baked")
    private boolean baked;

    @JsonProperty("cheese")
    private List<String> cheese;

    @JsonProperty("meat")
    private List<String> meat;

    @JsonProperty("veggies")
    private List<String> veggies;
}