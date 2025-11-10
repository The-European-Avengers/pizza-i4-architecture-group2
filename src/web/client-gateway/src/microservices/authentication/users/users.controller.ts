import { Controller, Get, Post, Body, Patch, Param, Delete, Inject } from '@nestjs/common';
import { CreateUserDto } from './dto/create-user.dto';
import { UpdateUserDto } from './dto/update-user.dto';
import { AUTHENTICATION_SERVICE } from 'src/config';
import { ClientProxy } from '@nestjs/microservices';

@Controller('users')
export class UsersController {
  constructor(@Inject(AUTHENTICATION_SERVICE) private readonly usersClient: ClientProxy) {}

  @Post()
  create(@Body() createUserDto: CreateUserDto) {
    return this.usersClient.send(
      { cmd: 'create_user' },
      createUserDto,
    );
  }

  @Get()
  findAll() {
    return this.usersClient.send({ cmd: 'get_all_users' },{});
  }

  // @Get(':id')
  // findOne(@Param('id') id: string) {
  //   return this.usersService.findOne(+id);
  // }

  // @Patch(':id')
  // update(@Param('id') id: string, @Body() updateUserDto: UpdateUserDto) {
  //   return this.usersService.update(+id, updateUserDto);
  // }

  // @Delete(':id')
  // remove(@Param('id') id: string) {
  //   return this.usersService.remove(+id);
  // }
}
