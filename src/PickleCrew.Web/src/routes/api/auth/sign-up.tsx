import { createFileRoute } from '@tanstack/react-router';
import { getSignUpUrl } from '@workos/authkit-tanstack-react-start';

export const Route = createFileRoute('/api/auth/sign-up')({
  server: {
    handlers: {
      GET: async ({ request }: { request: Request }) => {
        const returnPathname =
          new URL(request.url).searchParams.get('returnPathname') ?? '/dashboard';
        const url = await getSignUpUrl({ data: { returnPathname } });
        return new Response(null, {
          status: 307,
          headers: { Location: url },
        });
      },
    },
  },
});
