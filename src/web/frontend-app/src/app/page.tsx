"use client";

import React, { useState } from "react";
import Order from "./interfaces/Order";

import { v4 as uuidv4 } from "uuid";

const MENU = [
  { id: 1, name: "Margherita", desc: "Tomato, mozzarella, basil", price: 8.5 },
  {
    id: 2,
    name: "Pepperoni",
    desc: "Tomato, mozzarella, pepperoni",
    price: 9.5,
  },
  {
    id: 3,
    name: "Hawaiian",
    desc: "Tomato, mozzarella, ham, pineapple",
    price: 10,
  },
  {
    id: 4,
    name: "Veggie",
    desc: "Tomato, mozzarella, seasonal veggies",
    price: 9,
  },
];

export default function Home() {
  const [quantities, setQuantities] = useState(() => {
    const init: { [key: number]: number } = {};
    MENU.forEach((m) => (init[m.id] = 1));
    return init;
  });

  const [cart, setCart] = useState<
    { id: number; name: string; price: number; qty: number }[]
  >([]);

  function changeQty(id: number, v: number) {
    setQuantities((q) => ({ ...q, [id]: Math.max(1, v) }));
  }

  function addToCart(item: (typeof MENU)[0]) {
    const qty = quantities[item.id] ?? 1;
    setCart((c) => {
      const existing = c.find((x) => x.id === item.id);
      if (existing) {
        return c.map((x) =>
          x.id === item.id ? { ...x, qty: x.qty + qty } : x
        );
      }
      return [...c, { id: item.id, name: item.name, price: item.price, qty }];
    });
  }

  function removeFromCart(id: number) {
    setCart((c) => c.filter((x) => x.id !== id));
  }

  function updateCartQty(id: number, qty: number) {
    setCart((c) =>
      c.map((x) => (x.id === id ? { ...x, qty: Math.max(1, qty) } : x))
    );
  }

  const total = cart.reduce((s, it) => s + it.price * it.qty, 0);

  const confirmOrder = () => {
    const items = cart.map((item) => ({
      pizzaName: item.name,
      quantity: item.qty,
    }));

    const orderObject: Order = {
      OrderId: uuidv4(),
      items: items,
      total: total,
      createdAt: new Date(),
    };

    console.log("Order confirmed");
    console.log(cart);
    console.log("Order Details Object:");
    console.log(orderObject);
    //alert(`Order confirmed: $${total.toFixed(2)}`);
  };

  return (
    <main className="min-h-screen bg-gradient-to-b from-amber-50 via-yellow-50 to-red-50 text-gray-900 p-6">
      <header className="text-center mb-6">
        <h1 className="text-4xl font-extrabold text-amber-800">
          I4 PIZZA FACTORY
        </h1>
        <p className="text-sm text-amber-600">Choose your pizza and quantity</p>
      </header>

      <section className="max-w-6xl mx-auto grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-2">
          <h2 className="text-2xl font-semibold mb-4 text-amber-700">Menu</h2>
          {MENU.map((item) => (
            <div
              key={item.id}
              className="flex justify-between items-center bg-white/80 rounded-xl p-4 shadow-md mb-3 border-l-4 border-amber-300"
            >
              <div>
                <h3 className="text-lg font-bold text-amber-800">
                  {item.name}
                </h3>
                <p className="text-sm text-gray-600">{item.desc}</p>
                <p className="text-sm font-medium text-amber-700">
                  ${item.price.toFixed(2)}
                </p>
              </div>
              <div className="flex items-center gap-3 justify-center">
                <label className="flex flex-col text-sm text-gray-700">
                  <span className="mb-1">Quantity</span>
                  <input
                    className="w-20 p-1 border rounded text-center"
                    type="number"
                    min={1}
                    value={quantities[item.id]}
                    onChange={(e) => changeQty(item.id, Number(e.target.value))}
                  />
                </label>
                <button
                  className="bg-amber-500 text-white px-3 py-1 rounded hover:bg-amber-600"
                  onClick={() => addToCart(item)}
                >
                  Add
                </button>
              </div>
            </div>
          ))}
        </div>

        <aside className="bg-white/90 rounded-xl p-4 shadow-md">
          <h2 className="text-2xl font-semibold mb-3 text-amber-700">Cart</h2>
          {cart.length === 0 ? (
            <p className="text-gray-600">Your cart is empty</p>
          ) : (
            <div>
              {cart.map((it) => (
                <div
                  key={it.id}
                  className="flex justify-between items-center mb-3"
                >
                  <div>
                    <strong className="block text-amber-800">{it.name}</strong>
                    <div className="text-sm text-gray-600">
                      ${it.price.toFixed(2)} c/u
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <input
                      className="w-16 p-1 border rounded text-center"
                      type="number"
                      min={1}
                      value={it.qty}
                      onChange={(e) =>
                        updateCartQty(it.id, Number(e.target.value))
                      }
                    />
                    <button
                      className="text-sm text-red-600 hover:underline"
                      onClick={() => removeFromCart(it.id)}
                    >
                      Remove
                    </button>
                  </div>
                </div>
              ))}

              <div className="flex justify-between items-center mt-4 border-t pt-3">
                <strong>Total:</strong>
                <strong className="text-amber-800">${total.toFixed(2)}</strong>
              </div>
              <button
                className="w-full mt-4 bg-red-500 text-white py-2 rounded hover:bg-red-600"
                onClick={confirmOrder}
              >
                Confirm order
              </button>
            </div>
          )}
        </aside>
      </section>

      <footer className="text-center mt-8 text-sm text-amber-700">
        Thank you for choosing us 🍕
      </footer>
    </main>
  );
}
