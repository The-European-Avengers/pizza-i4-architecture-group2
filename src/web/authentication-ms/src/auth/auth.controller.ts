import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { AuthService } from './auth.service';
import { LoginUserDto } from './dto/login-user.dto';


@Controller()
export class AuthController {
  constructor(private readonly authService: AuthService) {}

  @MessagePattern({ cmd: 'login_user' })
  login(@Payload() loginUserDto:LoginUserDto){
    return this.authService.login(loginUserDto);
  }


  // @MessagePattern('createAuth')
  // create(@Payload() createAuthDto: any) {
  //   return this.authService.create(createAuthDto);
  // }

  // @MessagePattern('findAllAuth')
  // findAll() {
  //   return this.authService.findAll();
  // }

  // @MessagePattern('findOneAuth')
  // findOne(@Payload() id: number) {
  //   return this.authService.findOne(id);
  // }

  // @MessagePattern('updateAuth')
  // update(@Payload() updateAuthDto: any) {
  //   return this.authService.update(updateAuthDto.id, updateAuthDto);
  // }

  // @MessagePattern('removeAuth')
  // remove(@Payload() id: number) {
  //   return this.authService.remove(id);
  // }
}
