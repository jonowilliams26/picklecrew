import { createFileRoute } from '@tanstack/react-router';
import { getAuth, signOut } from '@workos/authkit-tanstack-react-start';

export const Route = createFileRoute('/api/auth/sign-out')({
  server: {
    handlers: {
      GET: async ({ request }: { request: Request }) => {
        const returnTo = new URL('/', request.url).toString();
        const { user } = await getAuth();

        if (!user) {
          return Response.redirect(returnTo, 307);
        }

        return signOut({ data: { returnTo } });
      },
    },
  },
});
