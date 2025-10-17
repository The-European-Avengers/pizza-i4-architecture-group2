import { Module } from '@nestjs/common';
import { UsersModule } from './microservices/authentication/users/users.module';

@Module({
  imports: [UsersModule],
  controllers: [],
  providers: [],
})
export class AppModule {}
