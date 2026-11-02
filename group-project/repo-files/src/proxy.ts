import createMiddleware from "next-intl/middleware";
import { createServerClient } from "@supabase/ssr";
import { NextResponse, type NextRequest } from "next/server";
import { routing } from "./i18n/routing";
import { supabaseConfigured, supabaseConfig } from "./lib/supabase/config";
const internationalize = createMiddleware(routing);
export async function proxy(request: NextRequest) {
  let response = request.nextUrl.pathname.startsWith("/auth/")
    ? NextResponse.next()
    : internationalize(request);
  response.headers.set("Cache-Control", "private, no-store");
  if (!supabaseConfigured()) return response;
  const { url, key } = supabaseConfig();
  const client = createServerClient(url, key, {
    cookies: {
      getAll: () => request.cookies.getAll(),
      setAll(values, headers) {
        values.forEach(({ name, value }) => request.cookies.set(name, value));
        response = request.nextUrl.pathname.startsWith("/auth/")
          ? NextResponse.next({ request })
          : internationalize(request);
        values.forEach(({ name, value, options }) => response.cookies.set(name, value, options));
        Object.entries(headers ?? {}).forEach(([name, value]) => response.headers.set(name, value));
      },
    },
  });
  await client.auth.getClaims();
  response.headers.set("Cache-Control", "private, no-store");
  return response;
}
// /api/* (the health check) is excluded so next-intl does not redirect it to /vi/api/*.
export const config = { matcher: ["/((?!api|_next|.*\\..*).*)"] };
