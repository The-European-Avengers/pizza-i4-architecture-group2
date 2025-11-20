import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { Logger, ValidationPipe } from '@nestjs/common';
import { MicroserviceOptions, Transport } from '@nestjs/microservices';
import { envs } from './config';

async function bootstrap() {
  const logger = new Logger('OrderingMS');

  // const app = await NestFactory.createMicroservice<MicroserviceOptions>(
  //   AppModule,
  //   {
  //     transport: Transport.TCP,
  //     options: { port: envs.port },
  //   },
  // );

  const app = await NestFactory.create(AppModule);

  // Configurar TCP para comunicación con API Gateway
  app.connectMicroservice<MicroserviceOptions>({
    transport: Transport.TCP,
    options: { port: envs.port },
  });

  // Configurar Kafka para eventos
  app.connectMicroservice<MicroserviceOptions>({
    transport: Transport.KAFKA,
    options: {
      client: {
        clientId: 'ordering-service',
        brokers: [process.env.KAFKA_BROKER || 'localhost:9092'],
      },
      consumer: {
        groupId: 'ordering-consumer-group',
      },
    },
  });

  await app.startAllMicroservices();

  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      forbidNonWhitelisted: true,
    }),
  );

  logger.log('🚀 Ordering microservice listening on TCP port 3001');
  logger.log('📨 Kafka consumer connected');

  // Opcional: también puedes tener un servidor HTTP para health checks
  await app.listen(envs.port);
  logger.log(`❤️ Health check available on HTTP port ${envs.port}`);
  logger.log(`Ordering microservice running on port ${envs.port}`);
}
bootstrap();
