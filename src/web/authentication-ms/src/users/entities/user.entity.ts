export class User {

    public id: number;

    public name: string;
    public lastname: string;
    public email: string;
    public password: string;
    public is_active: boolean;
    public role: 'ADMIN' | 'USER';

    public created_at: Date;
    public updated_at: Date;

}
