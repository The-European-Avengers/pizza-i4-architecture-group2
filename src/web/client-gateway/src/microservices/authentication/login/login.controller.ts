import {
  Controller,
  Get,
  Post,
  Body,
  Patch,
  Param,
  Delete,
  Inject,
} from '@nestjs/common';
import { LoginUserDto } from './dto/login-user.dto';
import { AUTHENTICATION_SERVICE } from 'src/config';
import { ClientProxy, RpcException } from '@nestjs/microservices';
import { catchError } from 'rxjs';

@Controller('login')
export class LoginController {
  constructor(
    @Inject(AUTHENTICATION_SERVICE) private readonly loginClient: ClientProxy,
  ) {}

  @Post()
  login(@Body() loginUserDto: LoginUserDto) {
    return this.loginClient.send({ cmd: 'login_user' }, loginUserDto).pipe(
      catchError((err) => {
         throw new RpcException(err);
      }),
    )
  }

  // @Post()
  // create(@Body() createLoginDto: CreateLoginDto) {
  //   return this.loginService.create(createLoginDto);
  // }

  // @Get()
  // findAll() {
  //   return this.loginService.findAll();
  // }

  // @Get(':id')
  // findOne(@Param('id') id: string) {
  //   return this.loginService.findOne(+id);
  // }

  // @Patch(':id')
  // update(@Param('id') id: string, @Body() updateLoginDto: UpdateLoginDto) {
  //   return this.loginService.update(+id, updateLoginDto);
  // }

  // @Delete(':id')
  // remove(@Param('id') id: string) {
  //   return this.loginService.remove(+id);
  // }
}
