
import * as joi from 'joi';


process.loadEnvFile()

interface EnvVars {
  PORT: number;
  //DATABASE_URL:string;
}



const envsSchema = joi
  .object({
    PORT: joi.number().required(),
    //DATABASE_URL: joi.string().required()
  })
  .unknown(true);



const {error, value}= envsSchema.validate(process.env)


if(error){
    throw new Error(`Config Validation error: ${error.message}`)
}

const envVars:EnvVars = value; //casteo


export const envs={
    port:envVars.PORT,
    //databaseUrl:envVars.DATABASE_URL
}


