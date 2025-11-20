import { Module } from '@nestjs/common';
import { UsersModule } from './microservices/authentication/users/users.module';
import { LoginModule } from './microservices/authentication/login/login.module';
import { OrdersModule } from './microservices/orders/orders.module';
import { OrderStackModule } from './microservices/order-stack/order-stack.module';

@Module({
  imports: [UsersModule, LoginModule, OrderStackModule],
  controllers: [],
  providers: [],
})
export class AppModule {}
