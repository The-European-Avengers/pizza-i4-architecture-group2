import { Module } from '@nestjs/common';

import { LoginController } from './login.controller';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { AUTHENTICATION_SERVICE, envs } from 'src/config';

@Module({
  controllers: [LoginController],
  providers: [],
  imports: [
    ClientsModule.register([
      {
        name: AUTHENTICATION_SERVICE,
        transport: Transport.TCP,
        options: {
          host: envs.authMicroserviceHost,
          port: envs.authMicroservicePort,
        },
      },
    ]),
  ],
})
export class LoginModule {}
