import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import { CreateUserDto } from './dto/create-user.dto';
import { UpdateUserDto } from './dto/update-user.dto';
import { PrismaClient } from 'generated/prisma';
import * as bcrypt from 'bcrypt';


@Injectable()
export class UsersService extends PrismaClient implements OnModuleInit {

 private readonly logger = new Logger('UsersService');

  onModuleInit() {
    this.$connect();
    this.logger.log('Database connected');
  }

  async create(createUserDto: CreateUserDto) {

    const { name,lastname,email,password, is_active, role} = createUserDto;

    try {

      const user = await this.user.findUnique({
        where: {
          email
        }
      });
      if (user) {
        throw new Error('User already exists');
      }
      
    } catch (error) {
      throw new Error(error.message);
    }



    return this.user.create({
      data: {
        name,
        lastname,
        email,
        password: bcrypt.hashSync(password, 10),
        is_active,
        role
      }
    });
  }

  async findAll() {

    const users= await this.user.findMany();
    return users;

  }

  findOne(id: number) {
    return `This action returns a #${id} user`;
  }

  update(id: number, updateUserDto: UpdateUserDto) {
    return `This action updates a #${id} user`;
  }

  remove(id: number) {
    return `This action removes a #${id} user`;
  }
}
