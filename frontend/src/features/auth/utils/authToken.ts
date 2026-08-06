import { jwtDecode } from "jwt-decode";

interface JwtClaims {
  exp?: number;
  sub?: string;
  email?: string;
  role?: string | string[];
}

export function isAccessTokenValid(token: string): boolean {
  try {
    const claims = jwtDecode<JwtClaims>(token);

    if (typeof claims.exp !== "number") {
      return false;
    }

    return claims.exp * 1000 > Date.now();
  } catch {
    return false;
  }
}
