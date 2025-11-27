import { LoginUserDto } from './dto/login-user.dto';
import { ClientProxy } from '@nestjs/microservices';
export declare class LoginController {
    private readonly loginClient;
    constructor(loginClient: ClientProxy);
    login(loginUserDto: LoginUserDto): import("rxjs").Observable<any>;
}
