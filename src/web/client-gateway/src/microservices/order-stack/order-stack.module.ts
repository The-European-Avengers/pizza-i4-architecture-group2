import { Module } from '@nestjs/common';

import { OrderStackController } from './order-stack.controller';
import { ClientsModule, Transport } from '@nestjs/microservices';

@Module({
  controllers: [OrderStackController],
  providers: [],
  imports: [
    ClientsModule.register([
      {
        name: 'KAFKA_SERVICE',
        transport: Transport.KAFKA,
        options: {
          client: {
            clientId: 'client-gateway',
            brokers: ['localhost:9092'],
          },
          consumer: {
            groupId: 'order-stack',
          },
        },
      },
    ]),
  ],
})
export class OrderStackModule {}
