//import { PrismaClient } from '../generated/prisma/client';

import { PrismaClient } from '../generated/prisma/client';

const prisma = new PrismaClient();

console.log('Seeding database...');

async function main() {
  // Clear existing data
  await prisma.order_Pizza.deleteMany();
  await prisma.recipe_Ingredient.deleteMany();
  await prisma.order.deleteMany();
  await prisma.pizza.deleteMany();
  await prisma.recipe.deleteMany();
  await prisma.ingredient.deleteMany();

  // Create Ingredients
  const ingredients = await Promise.all([
    prisma.ingredient.create({ data: { name: 'Tomato Sauce' } }),
    prisma.ingredient.create({ data: { name: 'Mozzarella Cheese' } }),
    prisma.ingredient.create({ data: { name: 'Pepperoni' } }),
    prisma.ingredient.create({ data: { name: 'Mushrooms' } }),
    prisma.ingredient.create({ data: { name: 'Bell Peppers' } }),
    prisma.ingredient.create({ data: { name: 'Onions' } }),
    prisma.ingredient.create({ data: { name: 'Black Olives' } }),
    prisma.ingredient.create({ data: { name: 'Italian Sausage' } }),
    prisma.ingredient.create({ data: { name: 'Basil' } }),
    prisma.ingredient.create({ data: { name: 'Parmesan Cheese' } }),
    prisma.ingredient.create({ data: { name: 'Bacon' } }),
    prisma.ingredient.create({ data: { name: 'Pineapple' } }),
    prisma.ingredient.create({ data: { name: 'Ham' } }),
  ]);

  // Create Recipes with Ingredients
  const margheritaRecipe = await prisma.recipe.create({
    data: {
      name: 'Margherita Recipe',
      ingredients: {
        create: [
          { id_ingredient: ingredients[0].id, quantity: 100 }, // Tomato Sauce
          { id_ingredient: ingredients[1].id, quantity: 150 }, // Mozzarella
          { id_ingredient: ingredients[8].id, quantity: 10 }, // Basil
        ],
      },
    },
  });

  const pepperoniRecipe = await prisma.recipe.create({
    data: {
      name: 'Pepperoni Recipe',
      ingredients: {
        create: [
          { id_ingredient: ingredients[0].id, quantity: 100 }, // Tomato Sauce
          { id_ingredient: ingredients[1].id, quantity: 150 }, // Mozzarella
          { id_ingredient: ingredients[2].id, quantity: 80 }, // Pepperoni
        ],
      },
    },
  });

  const vegetarianRecipe = await prisma.recipe.create({
    data: {
      name: 'Vegetarian Recipe',
      ingredients: {
        create: [
          { id_ingredient: ingredients[0].id, quantity: 100 }, // Tomato Sauce
          { id_ingredient: ingredients[1].id, quantity: 150 }, // Mozzarella
          { id_ingredient: ingredients[3].id, quantity: 50 }, // Mushrooms
          { id_ingredient: ingredients[4].id, quantity: 50 }, // Bell Peppers
          { id_ingredient: ingredients[5].id, quantity: 30 }, // Onions
          { id_ingredient: ingredients[6].id, quantity: 30 }, // Black Olives
        ],
      },
    },
  });

  const meatLoversRecipe = await prisma.recipe.create({
    data: {
      name: 'Meat Lovers Recipe',
      ingredients: {
        create: [
          { id_ingredient: ingredients[0].id, quantity: 100 }, // Tomato Sauce
          { id_ingredient: ingredients[1].id, quantity: 150 }, // Mozzarella
          { id_ingredient: ingredients[2].id, quantity: 60 }, // Pepperoni
          { id_ingredient: ingredients[7].id, quantity: 60 }, // Italian Sausage
          { id_ingredient: ingredients[10].id, quantity: 50 }, // Bacon
        ],
      },
    },
  });

  const hawaiianRecipe = await prisma.recipe.create({
    data: {
      name: 'Hawaiian Recipe',
      ingredients: {
        create: [
          { id_ingredient: ingredients[0].id, quantity: 100 }, // Tomato Sauce
          { id_ingredient: ingredients[1].id, quantity: 150 }, // Mozzarella
          { id_ingredient: ingredients[12].id, quantity: 80 }, // Ham
          { id_ingredient: ingredients[11].id, quantity: 80 }, // Pineapple
        ],
      },
    },
  });

  // Create Pizzas
  const pizzas = await Promise.all([
    prisma.pizza.create({
      data: {
        name: 'Margherita',
        unit_price: 9.99,
        id_recipe: margheritaRecipe.id,
      },
    }),
    prisma.pizza.create({
      data: {
        name: 'Pepperoni',
        unit_price: 12.99,
        id_recipe: pepperoniRecipe.id,
      },
    }),
    prisma.pizza.create({
      data: {
        name: 'Vegetarian Supreme',
        unit_price: 13.99,
        id_recipe: vegetarianRecipe.id,
      },
    }),
    prisma.pizza.create({
      data: {
        name: 'Meat Lovers',
        unit_price: 15.99,
        id_recipe: meatLoversRecipe.id,
      },
    }),
    prisma.pizza.create({
      data: {
        name: 'Hawaiian',
        unit_price: 13.99,
        id_recipe: hawaiianRecipe.id,
      },
    }),
  ]);

  // Create Orders
  const order1 = await prisma.order.create({
    data: {
      user_id: 'user-001',
      status: 'completed',
      pizzas: {
        create: [
          { id_pizza: pizzas[0].id, quantity: 2 }, // 2x Margherita
          { id_pizza: pizzas[1].id, quantity: 1 }, // 1x Pepperoni
        ],
      },
    },
  });

  const order2 = await prisma.order.create({
    data: {
      user_id: 'user-002',
      status: 'preparing',
      pizzas: {
        create: [
          { id_pizza: pizzas[3].id, quantity: 1 }, // 1x Meat Lovers
          { id_pizza: pizzas[2].id, quantity: 1 }, // 1x Vegetarian
        ],
      },
    },
  });

  const order3 = await prisma.order.create({
    data: {
      user_id: 'user-001',
      status: 'delivered',
      pizzas: {
        create: [
          { id_pizza: pizzas[4].id, quantity: 3 }, // 3x Hawaiian
        ],
      },
    },
  });

  const order4 = await prisma.order.create({
    data: {
      user_id: 'user-003',
      status: 'pending',
      pizzas: {
        create: [
          { id_pizza: pizzas[1].id, quantity: 2 }, // 2x Pepperoni
          { id_pizza: pizzas[3].id, quantity: 1 }, // 1x Meat Lovers
          { id_pizza: pizzas[0].id, quantity: 1 }, // 1x Margherita
        ],
      },
    },
  });

  console.log('Seed data created successfully!');
  console.log(`Created ${ingredients.length} ingredients`);
  console.log(`Created 5 recipes`);
  console.log(`Created ${pizzas.length} pizzas`);
  console.log(`Created 4 orders`);
}

main()
  .then(async () => {
    await prisma.$disconnect();
  })
  .catch(async (e) => {
    console.error(e);
    await prisma.$disconnect();
    process.exit(1);
  });
