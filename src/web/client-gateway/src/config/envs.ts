import * as joi from 'joi';

process.loadEnvFile();

interface EnvVars {
  PORT: number;
  AUTH_MICROSERVICE_HOST: string;
  AUTH_MICROSERVICE_PORT: number;
  ORDERING_MICROSERVICE_HOST: string;
  ORDERING_MICROSERVICE_PORT: number;
}

const envsSchema = joi
  .object({
    PORT: joi.number().required(),
    AUTH_MICROSERVICE_HOST: joi.string().required(),
    AUTH_MICROSERVICE_PORT: joi.number().required(),

  // Added ordering microservice env var
    ORDERING_MICROSERVICE_HOST: joi.string().required(),
    ORDERING_MICROSERVICE_PORT: joi.number().required(),
  
  })
  .unknown(true);

const { error, value } = envsSchema.validate(process.env);

if (error) {
  throw new Error(`Config Validation error: ${error.message}`);
}

const envVars: EnvVars = value;

export const envs = {
  port: envVars.PORT,
  authMicroserviceHost: envVars.AUTH_MICROSERVICE_HOST,
  authMicroservicePort: envVars.AUTH_MICROSERVICE_PORT,
  orderingMicroserviceHost: envVars.ORDERING_MICROSERVICE_HOST,
  orderingMicroservicePort: envVars.ORDERING_MICROSERVICE_PORT,
};
