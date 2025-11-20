import { PartialType } from '@nestjs/mapped-types';
import { CreateOrderStackDto } from './create-order-stack.dto';

export class UpdateOrderStackDto extends PartialType(CreateOrderStackDto) {
  id: number;
}
