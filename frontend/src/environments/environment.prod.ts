export const environment = {
  production: true,
  // IMPORTANT: must be a keshavsingh.in subdomain so the shared SSO cookie (domain .keshavsingh.in)
  // is sent with credentialed requests — admin.keshavsingh.in is only the frontend SPA (GitHub
  // Pages/Fastly, no API behind it); id.keshavsingh.in is the actual Render-hosted API domain.
  idpUrl: 'https://id.keshavsingh.in/api',
  apiUrl: 'https://pratiyogita.onrender.com/api',
  loginUrl: 'https://admin.keshavsingh.in/login',
};
