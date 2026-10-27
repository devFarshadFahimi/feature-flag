/** Reference for the server's deterministic assignment, not an SDK or evaluation client.
 * Compare with rollout-buckets.json. Strings use UTF-8 byte lengths, never JS character counts.
 * Canonical UUID D format is lowercase. The two purposes deliberately produce separate buckets. */
export async function rolloutBucket(
  projectId: string,
  group: string,
  field: string,
  value: string,
  purpose: "rollout" | "variant",
): Promise<number> {
  const encoder = new TextEncoder();
  const parts = ["v1", projectId.toLowerCase(), group, field, value, purpose].map(p => encoder.encode(p));
  const input = new Uint8Array(parts.reduce((size, part) => size + 4 + part.length, 0));
  const view = new DataView(input.buffer);
  let offset = 0;

  for (const part of parts) {
    view.setUint32(offset, part.length, false);
    offset += 4;
    input.set(part, offset);
    offset += part.length;
  }

  const digest = await crypto.subtle.digest("SHA-256", input);

  return Number(new DataView(digest).getBigUint64(0, false) % 10000n);
}
