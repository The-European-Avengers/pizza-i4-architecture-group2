export declare class CreateUserDto {
    name: string;
    lastname: string;
    email: string;
    password: string;
    is_active: boolean;
    role: 'ADMIN' | 'USER';
}
