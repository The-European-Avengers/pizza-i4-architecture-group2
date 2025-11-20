import { Controller, Get, Inject, Post } from '@nestjs/common';
import { ClientProxy, MessagePattern, Payload } from '@nestjs/microservices';

import { CreateOrderDto } from './dto/create-order.dto';
import { UpdateOrderDto } from './dto/update-order.dto';
import { ORDERING_SERVICE } from 'src/config/services';

@Controller('orders')
export class OrdersController {
  constructor(
    @Inject(ORDERING_SERVICE) private readonly ordersClient: ClientProxy,
  ) {}

  @Post()
  create() {
    return this.ordersClient.send('createOrder', {});
  }

  @Get()
  findAll() {
    return this.ordersClient.send('findAllOrders', {});
  }

  // @MessagePattern('findOneOrder')
  // findOne(@Payload() id: number) {
  //   return this.ordersClient.send('findOneOrder', id);
  // }

  // @MessagePattern('updateOrder')
  // update(@Payload() updateOrderDto: UpdateOrderDto) {
  //   return this.ordersClient.send('updateOrder', updateOrderDto);
  // }

  // @MessagePattern('removeOrder')
  // remove(@Payload() id: number) {
  //   return this.ordersClient.send('removeOrder', id);
  // }
}
