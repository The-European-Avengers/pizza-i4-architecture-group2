import { v4 as uuidv4 } from "uuid";

const CLIENTGATEWAY_API_URL_ORDER =
  "http://localhost:3000/api/order-stack/order";

type Order = {
  OrderId: string;
  pizzas:Record<string, number>;
  // items: {
  //   pizzaName: string;
  //   quantity: number;
  // }[];
  isBaked?: boolean;
  createdAt: Date;
}

type Pizza = {
  id: number;
  name: string;
  desc: string;
  price: number;
}

const MENU: Pizza[] = [
  { id: 1, name: "Margherita", desc: "Tomato, mozzarella, basil", price: 8.5 },
  {
    id: 2,
    name: "Pepperoni Classic",
    desc: "Tomato, mozzarella, pepperoni",
    price: 9.5,
  },
  {
    id: 3,
    name: "Supreme Deluxe",
    desc: "Tomato, mozzarella, ham, pineapple",
    price: 10,
  },
  {
    id: 4,
    name: "BBQ Chicken Ranch",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
  {
    id: 5,
    name: "Vegetarian Pesto",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
  {
    id: 6,
    name: "Four Cheese (Quattro Formaggi)",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
  {
    id: 7,
    name: "Hawaiian Delight",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
  {
    id: 8,
    name: "Spicy Sriracha Beef",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
  {
    id: 9,
    name: "Truffle Mushroom",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
  {
    id: 10,
    name: "Breakfast Pizza",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
];

const getRandomInt = (min: number, max: number): number => {
  return Math.floor(Math.random() * (max - min + 1)) + min;
};

//Select the amount of pizzas in the order
const createRandomOrder = (numberOfTypes: number): Order => {
  const MAX_TOTAL_PIZZAS = 100;

  const shuffledMenu = [...MENU].sort(() => 0.5 - Math.random());

  const selectedPizzas = shuffledMenu.slice(0, numberOfTypes);

  let calculatedTotal = 0;
  let currentPizzaCount = 0;

  const orderItems = selectedPizzas
    .map((pizza) => {
      const remainingSpace = MAX_TOTAL_PIZZAS - currentPizzaCount;

      if (remainingSpace <= 0) return null;

      const maxQuantity = Math.min(5, remainingSpace);
      const quantity = getRandomInt(1, maxQuantity);

      currentPizzaCount += quantity;
      calculatedTotal += pizza.price * quantity;

      return {
        pizzaName: pizza.name,
        quantity: quantity,
      };
    })
    .filter((item) => item !== null);

  console.log(`Total Pizzas in Order: ${currentPizzaCount}`);
  console.log("-----------------");

  const pizzasRecord: Record<string, number> = {};
  orderItems.forEach(item => {
    if (item) {
      pizzasRecord[item.pizzaName] = item.quantity;
    }
  });

  return {
    OrderId: uuidv4(),
    pizzas: pizzasRecord,
    isBaked: true,
    //total: parseFloat(calculatedTotal.toFixed(2)),
    createdAt: new Date(),
  };
};

const startOrderGeneration = async () => {
  if (process.argv[2] > MENU.length || process.argv[2] < 1) {
    console.error("Please provide a correct number of pizzas");
    process.exit(1);
  }

  if (process.argv.length < 3) {
    console.error("Please provide the number of different pizza types.");
    process.exit(1);
  }
  const amount = parseInt(process.argv[2]);

  try {
    console.log("--- Random Order ---");

    const myOrder = createRandomOrder(amount);

    console.log(JSON.stringify(myOrder, null, 2));

    const resp = await fetch(CLIENTGATEWAY_API_URL_ORDER, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(myOrder),
    });

    const data = await resp.json();
    console.log("--- Response ---");
    console.log(data);
  } catch (error) {
    console.error("Error creating order:", error);
  }
};

startOrderGeneration();
