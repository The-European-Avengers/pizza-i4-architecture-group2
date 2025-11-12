import { IsBoolean, IsEnum, IsString } from "class-validator";

export class CreateUserDto {
     @IsString()
        public name:string;

        @IsString()
        public lastname:string;

        @IsString()
        public email:string;

        @IsString()
        public password:string;


        @IsBoolean()
        public is_active:boolean;

  
        @IsEnum(['ADMIN','USER'])
        public role: 'ADMIN' | 'USER';

}
