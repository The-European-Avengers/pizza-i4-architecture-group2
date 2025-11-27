"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UpdateOrderStackDto = void 0;
const mapped_types_1 = require("@nestjs/mapped-types");
const create_order_stack_dto_1 = require("./create-order-stack.dto");
class UpdateOrderStackDto extends (0, mapped_types_1.PartialType)(create_order_stack_dto_1.CreateOrderStackDto) {
    id;
}
exports.UpdateOrderStackDto = UpdateOrderStackDto;
//# sourceMappingURL=update-order-stack.dto.js.map