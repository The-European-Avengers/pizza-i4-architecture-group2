import { Module } from '@nestjs/common';
import { UsersModule } from './microservices/authentication/users/users.module';
import { LoginModule } from './microservices/authentication/login/login.module';

@Module({
  imports: [UsersModule, LoginModule],
  controllers: [],
  providers: [],
})
export class AppModule {}
