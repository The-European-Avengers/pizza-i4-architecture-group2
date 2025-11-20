import NextAuth, { type NextAuthConfig } from "next-auth";

import Credentials from "next-auth/providers/credentials";
import { z } from "zod";

export const authConfig: NextAuthConfig = {
  pages: {
    signIn: "/login",
    newUser: "/register",
  },
  providers: [
    Credentials({
      async authorize(credentials) {
        const parsedCredentials = z
          .object({ email: z.email(), password: z.string().min(6) })
          .safeParse(credentials);

        if (!parsedCredentials.success) {
          throw new Error("Invalid credentials");
        }

        console.log(parsedCredentials.success)
        const { email, password } = parsedCredentials.data;

        console.log("From AuthConfig.ts")
        console.log("Email:", email);
        console.log("Password:", password);

        //TODO: Implement your own authentication logic here

        return null;
      },
    }),
  ],
};

export const { signIn, signOut, auth } = NextAuth(authConfig);
