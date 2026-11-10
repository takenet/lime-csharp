# Message trace id (Lime)

Messages are correlated across applications by the **W3C trace-id** already carried in `Metadata["traceparent"]`.
No new envelope field or wire format is introduced.

- `envelope.GetTraceId()` returns the 32-hex trace-id of the `traceparent` (or `null` when absent/invalid).
- `StartActivity(envelope)` tags the span with `lime.message_trace_id` (the id carried by the envelope, read before the
  span rewrites `traceparent`) next to the existing `lime.envelope.id`, `lime.envelope.from/to/type` tags.
- Applications log it as `MessageTraceId` (see iris-shared) only from envelope-processing scopes; the ambient
  `trace_id` log field keeps meaning "current activity".

Note: messages sent by a bot flow inside the same activity share the same parent span-id. The trace-id is the
correlation key; use it together with `envelope.Id` to identify a single message.

## Tempo queries

- Whole message flow: open the trace by id in Tempo (the message trace-id is the Tempo trace id).
- One specific message: `{ span.lime.envelope.id = "<envelope id>" }`.
- Spans of a message even when the local span was started under another trace (an ambient activity exists, so the
  span's own trace-id differs from the envelope's): `{ span.lime.message_trace_id = "<trace id>" }`.
