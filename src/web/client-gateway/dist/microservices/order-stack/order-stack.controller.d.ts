import { ClientKafka } from '@nestjs/microservices';
export declare class OrderStackController {
    private readonly kafkaClient;
    constructor(kafkaClient: ClientKafka);
    onModuleInit(): Promise<void>;
    sendOrder(message: any): Promise<{
        success: boolean;
        message: string;
        newOrder: any;
        error?: undefined;
    } | {
        success: boolean;
        message: string;
        error: any;
        newOrder?: undefined;
    }>;
}
