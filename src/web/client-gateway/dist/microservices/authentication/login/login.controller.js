"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.LoginController = void 0;
const common_1 = require("@nestjs/common");
const login_user_dto_1 = require("./dto/login-user.dto");
const config_1 = require("../../../config");
const microservices_1 = require("@nestjs/microservices");
const rxjs_1 = require("rxjs");
let LoginController = class LoginController {
    loginClient;
    constructor(loginClient) {
        this.loginClient = loginClient;
    }
    login(loginUserDto) {
        return this.loginClient.send({ cmd: 'login_user' }, loginUserDto).pipe((0, rxjs_1.catchError)((err) => {
            throw new microservices_1.RpcException(err);
        }));
    }
};
exports.LoginController = LoginController;
__decorate([
    (0, common_1.Post)(),
    __param(0, (0, common_1.Body)()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [login_user_dto_1.LoginUserDto]),
    __metadata("design:returntype", void 0)
], LoginController.prototype, "login", null);
exports.LoginController = LoginController = __decorate([
    (0, common_1.Controller)('login'),
    __param(0, (0, common_1.Inject)(config_1.AUTHENTICATION_SERVICE)),
    __metadata("design:paramtypes", [microservices_1.ClientProxy])
], LoginController);
//# sourceMappingURL=login.controller.js.map