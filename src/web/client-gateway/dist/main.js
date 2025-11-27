"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const core_1 = require("@nestjs/core");
const app_module_1 = require("./app.module");
const common_1 = require("@nestjs/common");
const config_1 = require("./config");
const rpc_custom_exception_filter_1 = require("./common/exceptions/rpc-custom-exception.filter");
async function bootstrap() {
    const logger = new common_1.Logger('Main-Gateway');
    const app = await core_1.NestFactory.create(app_module_1.AppModule);
    app.setGlobalPrefix('api');
    app.useGlobalPipes(new common_1.ValidationPipe({
        whitelist: true,
        forbidNonWhitelisted: true,
    }));
    app.useGlobalFilters(new rpc_custom_exception_filter_1.RpcCustomExceptionFilter());
    await app.listen(config_1.envs.port);
    logger.log(`Client gateway listening on port ${config_1.envs.port}`);
}
bootstrap();
//# sourceMappingURL=main.js.map