import { HttpStatus, Injectable, Logger, OnModuleInit } from '@nestjs/common';
import { LoginUserDto } from './dto/login-user.dto';
import { PrismaClient } from 'generated/prisma';
import * as bcrypt from 'bcrypt';
import { RpcException } from '@nestjs/microservices';

@Injectable()
export class AuthService extends PrismaClient implements OnModuleInit {
  private readonly logger = new Logger('AuthService');

  onModuleInit() {
    this.$connect();
    this.logger.log('Database connected');
  }

  async login(loginUserDto: LoginUserDto) {
    const { email, password } = loginUserDto;

    try {
      const user = await this.user.findUnique({
        where: { email },
      });

      if (!user) {
        throw new RpcException({
          message: `User with email ${email} not found`,
          status: HttpStatus.NOT_FOUND,
        });
      }

      const isPasswordValid = bcrypt.compareSync(password, user.password);

      if (!isPasswordValid) {
        throw new RpcException({
          message: `Email or password is incorrect`,
          status: HttpStatus.BAD_REQUEST,
        });
      }

      const { password: _, ...rest } = user;

      return {
        user: rest,
        token: 'ABC',
      };
    } catch (error) {
      throw new RpcException(error);
    }
  }

  create(createAuthDto: any) {
    return 'This action adds a new auth';
  }

  findAll() {
    return `This action returns all auth`;
  }

  findOne(id: number) {
    return `This action returns a #${id} auth`;
  }

  update(id: number, updateAuthDto: any) {
    return `This action updates a #${id} auth`;
  }

  remove(id: number) {
    return `This action removes a #${id} auth`;
  }
}
