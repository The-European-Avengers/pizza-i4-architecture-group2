import { CreateUserDto } from './dto/create-user.dto';
import { ClientProxy } from '@nestjs/microservices';
export declare class UsersController {
    private readonly usersClient;
    constructor(usersClient: ClientProxy);
    create(createUserDto: CreateUserDto): import("rxjs").Observable<any>;
    findAll(): import("rxjs").Observable<any>;
}
