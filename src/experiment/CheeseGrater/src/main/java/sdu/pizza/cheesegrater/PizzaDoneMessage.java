package sdu.pizza.cheesegrater;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;

@Data
public class PizzaDoneMessage {
    @JsonProperty("pizzaId")
    private int pizzaId;

    @JsonProperty("orderId")
    private int orderId;

    @JsonProperty("doneMsg")
    private boolean doneMsg = true;
}
