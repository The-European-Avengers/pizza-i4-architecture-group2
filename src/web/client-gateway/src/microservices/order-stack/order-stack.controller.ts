import { Body, Controller, Inject, Post } from '@nestjs/common';
import { ClientKafka, MessagePattern, Payload } from '@nestjs/microservices';

import { CreateOrderStackDto } from './dto/create-order-stack.dto';
import { UpdateOrderStackDto } from './dto/update-order-stack.dto';

@Controller('order-stack')
export class OrderStackController {
  constructor(
    @Inject('KAFKA_SERVICE') private readonly kafkaClient: ClientKafka,
  ) {}

  async onModuleInit() {
    await this.kafkaClient.connect();
  }

  @Post('order')
  async sendOrder(@Body() message: any) {
    
    this.kafkaClient.emit('order-stack', message);

    return {
      success: true,
      message: 'Order sent to Kafka topic: order-stack',
      newOrder: message,
    };
  }
}
