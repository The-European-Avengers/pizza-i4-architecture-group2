"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.OrderStackModule = void 0;
const common_1 = require("@nestjs/common");
const order_stack_controller_1 = require("./order-stack.controller");
const microservices_1 = require("@nestjs/microservices");
let OrderStackModule = class OrderStackModule {
};
exports.OrderStackModule = OrderStackModule;
exports.OrderStackModule = OrderStackModule = __decorate([
    (0, common_1.Module)({
        controllers: [order_stack_controller_1.OrderStackController],
        providers: [],
        imports: [
            microservices_1.ClientsModule.register([
                {
                    name: 'KAFKA_SERVICE',
                    transport: microservices_1.Transport.KAFKA,
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
], OrderStackModule);
//# sourceMappingURL=order-stack.module.js.map