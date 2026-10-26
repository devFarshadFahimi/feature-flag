/** Browser example: call YOUR backend's /features route. Its authenticated session provides a JWT
 * accepted by feature-flag. No SDK, application key or configuration rules belong in the browser.
 * This standalone demo accepts an existing user token; production should use your backend's secure
 * session mechanism and remove this parameter. Never persist it in localStorage.
 */
export async function getFeatures(names: string[], userToken: string, signal?: AbortSignal) {
  const query = new URLSearchParams();
  for (const name of names) query.append("name", name);
  const response = await fetch(`/features?${query}`, {
    headers: { Authorization: `Bearer ${userToken}` },
    credentials: "same-origin",
    cache: "no-store",
    signal
  });
  if (!response.ok) throw new Error("دریافت وضعیت قابلیت‌ها ممکن نیست.");
  return await response.json() as {
    features: Array<{ name: string; enabled: boolean; variant: { name: string; enabled: boolean; payload: Record<string, unknown> | null }; reason: string }>;
    fallback: boolean;
  };
}
