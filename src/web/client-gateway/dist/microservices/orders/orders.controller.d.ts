import { ClientProxy } from '@nestjs/microservices';
export declare class OrdersController {
    private readonly ordersClient;
    constructor(ordersClient: ClientProxy);
    create(): import("rxjs").Observable<any>;
    findAll(): import("rxjs").Observable<any>;
}
