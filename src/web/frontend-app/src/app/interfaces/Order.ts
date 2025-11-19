interface Order {
  OrderId: string;
  items: {
    pizzaName: string;
    quantity: number;
  }[];
  total: number;
  createdAt: Date;
}
export default Order;
