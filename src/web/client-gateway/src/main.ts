import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { Logger, ValidationPipe } from '@nestjs/common';
import { envs } from './config';
import { RpcCustomExceptionFilter } from './common/exceptions/rpc-custom-exception.filter';

async function bootstrap() {
  const logger = new Logger('Main-Gateway');
  const app = await NestFactory.create(AppModule);

  app.setGlobalPrefix('api');


  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      forbidNonWhitelisted: true,
    }),
  );

  app.useGlobalFilters(new RpcCustomExceptionFilter()) //filtro global para excepciones de microservicios
  await app.listen(envs.port);

  logger.log(`Client gateway listening on port ${envs.port}`);
}
bootstrap();
