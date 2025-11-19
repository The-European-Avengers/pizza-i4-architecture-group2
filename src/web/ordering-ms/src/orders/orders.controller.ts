import { Controller, Inject } from '@nestjs/common';
import { MessagePattern, Payload, ClientKafka } from '@nestjs/microservices';
import { OrdersService } from './orders.service';
import { CreateOrderDto } from './dto/create-order.dto';
import { UpdateOrderDto } from './dto/update-order.dto';

@Controller()
export class OrdersController {
  constructor(
    private readonly ordersService: OrdersService,
    @Inject('KAFKA_SERVICE') private readonly kafkaClient: ClientKafka,
  ) {}

  @MessagePattern('createOrder')
  async create(@Payload() createOrderDto: CreateOrderDto) {
    //const order = await this.ordersService.create(createOrderDto);

    const newOrder = {
      orderId: 123,
      orderSize: 2,
      startTimestamp: new Date().toISOString(),
    };

    // Emit an event to Kafka after creating the order
    // this.kafkaClient.emit('order-done', {
    //   orderId: order.id,
    //   userId: order.userId,
    //   total: order.total,
    //   timestamp: new Date().toISOString(),
    // });

    this.kafkaClient.emit('order-processing', {
      orderId: newOrder.orderId,
      orderSize: newOrder.orderSize,
      startTimestamp: newOrder.startTimestamp,
    });

    return newOrder;
  }

  @MessagePattern('findAllOrders')
  findAll() {
    return this.ordersService.findAll();
  }

  @MessagePattern('findOneOrder')
  findOne(@Payload() id: number) {
    return this.ordersService.findOne(id);
  }

  @MessagePattern('updateOrder')
  update(@Payload() updateOrderDto: UpdateOrderDto) {
    return this.ordersService.update(updateOrderDto.id, updateOrderDto);
  }

  @MessagePattern('removeOrder')
  remove(@Payload() id: number) {
    return this.ordersService.remove(id);
  }
}
