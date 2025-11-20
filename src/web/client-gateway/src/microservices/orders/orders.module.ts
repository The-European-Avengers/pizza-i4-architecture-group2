import { Module } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import {  envs, ORDERING_SERVICE } from 'src/config';
import { OrdersController } from './orders.controller';

@Module({
  controllers: [OrdersController],
  providers: [],
  imports: [
    ClientsModule.register([
      {
        name: ORDERING_SERVICE,
        transport: Transport.TCP,
        options: {
          host: envs.orderingMicroserviceHost,
          port: envs.orderingMicroservicePort,
        },
      },
    ]),
  ],
})
export class OrdersModule {}
