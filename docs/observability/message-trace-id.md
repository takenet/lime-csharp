# Message trace id (Lime)

Messages are correlated across applications by the **W3C trace-id** already carried in `Metadata["traceparent"]`.
No new envelope field or wire format is introduced.

- `envelope.GetTraceId()` returns the 32-hex trace-id of the `traceparent` (or `null` when absent/invalid).
- `StartActivity(envelope)` tags the activity with `lime.message_trace_id`, so message spans can be found in Tempo
  (`lime.message_trace_id = "<id>"`) and told apart from endpoint/method spans that share the same trace-id format.
- Applications log it as `MessageTraceId` (see iris-shared) only from envelope-processing scopes; the ambient
  `trace_id` log field keeps meaning "current activity".

Note: messages sent by a bot flow inside the same activity share the same parent span-id. The trace-id is the
correlation key; use it together with `envelope.Id` to identify a single message.
