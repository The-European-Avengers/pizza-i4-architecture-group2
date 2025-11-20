import { Catch, ArgumentsHost, ExceptionFilter } from '@nestjs/common';
import { RpcException } from '@nestjs/microservices';

@Catch(RpcException)
export class RpcCustomExceptionFilter implements ExceptionFilter {
  catch(exception: RpcException, host: ArgumentsHost) {

    //console.log('RPC EXCEPTION CAPTURADA POR EL FILTRO CUSTOM');
    const ctx= host.switchToHttp();
    const response= ctx.getResponse();
    
    const rpcError= exception.getError();
    console.log(rpcError);

    if(typeof rpcError === 'object' && 'status' in rpcError && 'message' in rpcError){
      
      const status= rpcError.status;
      return response.status(status).json(rpcError)
    }


    response.status(400).json({
        statusCode: 400,
        message:rpcError
      })


  }
}