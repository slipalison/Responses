---
name: csharp
description: "usar quando for gerar codigos em csharp"
paths:
  - "**/*.cs"
---
 
# C# Rule — hot-path service (MANDATORY for all AI-generated C#)
 
Applies to EVERY C# change in this repo (Copilot, Claude Code, Gemini, OpenCode, any assistant).
This API is a hot path: thousands of requests/s. Allocation and exception discipline are not optional.
Priority when rules conflict: **1) Security 2) Performance 3) Clean Code / SOLID / DRY / YAGNI**.
Complements `.claude/rules/code-quality.md` (SonarQube) — both apply.
 
## 1. Flow control — Result pattern, NEVER exceptions
 
- NEVER throw for expected outcomes (validation failure, business rejection, not-found, missing context, tenant rules). Return the existing result types: `ToolResult.Fail(...)` in handlers/adapters, `ValidationResult.Fail(...)` in validators, `bool TryXxx(out T)` for new internal APIs.
- Exceptions are ONLY for: programmer bugs (`ArgumentNullException` at public entry), unrecoverable infra faults, cancellation (`OperationCanceledException`). Exactly ONE catch at the boundary (tool/controller/handler) converts to `ToolResult.Fail`/`ProblemDetails`; `ExceptionMiddleware` owns the rest.
- NEVER throw-and-catch inside your own call chain to signal a condition. NEVER branch logic in a `catch`. NEVER `catch { }` — comment or log (S108).
- Why (fixed): throw+unwind costs ~1000x a returned Result, allocates, and pollutes traces.
## 2. Allocation, memory & GC — hot path
 
Applies to **per-request code**: middleware, tools, handlers, validators, adapters, `BlipProcessingService`, serialization, event tracking. Three principles, in priority order: **① don't allocate if you can avoid it ② reuse memory over allocating new ③ stack beats heap.** Every allocation is deferred GC work — fewer allocations means fewer gen0 collections and predictable p99 latency. **Measure before optimizing** (BenchmarkDotNet, `dotnet-counters`, `dotnet-gcdump`); do not micro-optimize on intuition (YAGNI). The rules below are the non-negotiable defaults — anything past them needs a benchmark.
 
> Complements `code-quality.md`, which owns the Roslyn/Sonar mechanics: S1192 (repeated literal → constant), SYSLIB1045 (`[GeneratedRegex]`), CA1854 (`TryGetValue` over `ContainsKey`+indexer), S6610/CA1866 (`StartsWith('c')` char overload). Those apply here too.
 
### 2.1 Strings — the #1 allocation source
 
Strings are immutable: every `+`, `$"..."`, `Substring`, `Trim`, `ToLower`, `Replace`, and `Split` allocates a **new** heap string. At thousands of req/s that is millions of dead gen0 objects per second. Do not produce strings you do not need.
 
**Compare / test without allocating** — always pass a `StringComparison`, never case-fold to compare:
```csharp
// ❌ allocates two upper-cased copies, and is culture-buggy
if (a.ToLower() == b.ToLower()) ...
// ✅ zero-alloc, deterministic
if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) ...
 
// ❌ Substring allocates just to test a prefix
if (path.Substring(0, 5) == "file:") ...
// ✅ span slice + char overload, zero-alloc
if (path.AsSpan().StartsWith("file:", StringComparison.Ordinal)) ...
if (value.StartsWith('[')) ...        // char overload — not "["
```
Use `Ordinal` for keys/ids/tokens/enum-like values, `OrdinalIgnoreCase` for case-insensitive; reserve culture-aware comparison for user-facing display sorting only.
 
**Slice / parse without allocating** — `ReadOnlySpan<char>`, never `Substring`/`Split` in the hot path:
```csharp
// ❌ Substring + concat → 3 temporary strings
var key = text.Substring(10) + "-" + text.Substring(0, 5);
// ✅ CA1845: span overloads → only the result allocates
var key = string.Concat(text.AsSpan(10), "-", text.AsSpan(0, 5));
 
// ❌ Split allocates a string[] plus one string per token
foreach (var part in csv.Split(',')) ...
// ✅ .NET 8 span split → zero-alloc enumeration
foreach (Range r in csv.AsSpan().Split(','))
{
    ReadOnlySpan<char> part = csv.AsSpan()[r];
    ...
}
```
Search/trim on the span too: `AsSpan().IndexOf(...)`, `.Slice(...)`, `.Trim()` (span `Trim` returns a span, no copy). `Span<char>`/`ReadOnlySpan<char>` **cannot cross `await`, be returned, or stored in a field** — for async lifetime use `ReadOnlyMemory<char>` (see §2.3).
 
**Build once, never piece by piece:**
```csharp
// ❌ interpolation/concat in a loop → N intermediate strings
var s = "";
foreach (var x in items) s += x + ";";
// ✅ StringBuilder, pre-sized — for any loop or ~5–10+ parts
var sb = new StringBuilder(items.Count * 8);
foreach (var x in items) sb.Append(x).Append(';');
var s = sb.ToString();
 
// ✅ final length known → string.Create fills the buffer in one shot (1 alloc)
var masked = string.Create(cpf.Length, cpf, static (dst, src) =>
{
    src.AsSpan(0, 3).CopyTo(dst);
    dst[3..].Fill('*');
});
```
- A single `$"{a}-{b}"` for one string is fine — C# 10+ lowers it to `DefaultInterpolatedStringHandler` (pooled internal buffer). What is banned is interpolation/`+` **in loops or repeated per-request builds**.
- For logs, **never** build the message — pass a Serilog message template (`"... {Prop}"`); the sink skips the string entirely when the level is off (§5).
- `string.IsNullOrEmpty` / `string.IsNullOrWhiteSpace`, never `.Trim().Length == 0`.
**Fixed string sets** (see also §2.4): repeated literal 3+ times → `const` (S1192); fixed lookup set → `static readonly FrozenSet<string>`/`FrozenDictionary` built once; fixed regex → `[GeneratedRegex]` on a `partial` method (SYSLIB1045), never `new Regex(...)` per call. **Never `string.Intern()` in the hot path** — interned strings are never collected (permanent leak); use `FrozenSet`/`const`.
 
### 2.2 Reduce GC pressure — allocate less, reuse, prefer value types
 
- **`class` → `struct` for small, short-lived, immutable data.** Value types live on the stack or inline in their owner — no heap allocation, no gen0 work (~6.5× cheaper to create). Keep them small (a few fields, ~≤16–24 bytes); use `readonly record struct`; pass **large** structs by `in` to avoid copies. Never let a struct escape as `object`/interface inside a loop — that boxes it (silent heap alloc) and defeats the point.
- **Never add a finalizer/destructor to a hot-path type.** Finalizable objects survive the first collection (promoted to gen1+) and are drained on a single finalizer thread — measured **~320× slower** to reclaim. Use `IDisposable` + `using`; wrap *native* handles with `SafeHandle`, which handles this correctly.
- **Large allocations (≥ 85,000 bytes) land on the Large Object Heap** — the single biggest source of memory bloat and OOM in this service. Keep them off it: see §2.7.
- **Pool transient large buffers and expensive instances:**
  - `ArrayPool<T>.Shared.Rent(n)` + `Return` in `try/finally` for large short-lived arrays (~51 ns pooled vs ~404 ns fresh). Never touch a buffer after `Return`; never `Return` one you did not `Rent`.
  - `Microsoft.Extensions.ObjectPool.ObjectPool<StringBuilder>` for high-frequency builders — rent, append, `ToString()`, return.
- **No boxing in hot loops:** do not pass value types as `object`, call them through an interface in a loop, or stuff them into `Dictionary<string, object>` when a typed shape works. Each box is a heap allocation.
- **No capturing closures in the hot path:** use `static` lambdas (the compiler then forbids capture) and cache delegates as `static readonly`. A capturing lambda allocates a closure object per call.
### 2.3 Buffers
- Temp buffer ≤ 256 bytes, single scope: `Span<byte> buf = stackalloc byte[128];` — ~2× faster than a heap array, freed on scope exit, zero GC. Larger or async-lived: `ArrayPool<T>` / `MemoryPool<T>` / `IMemoryOwner<T>`.
- A `Span<T>` (including `stackalloc`) **cannot be returned, stored in a field, or cross `await`** — the compiler enforces `ref` safety. Cross those boundaries with `Memory<T>`/`ReadOnlyMemory<T>`.
### 2.4 Collections / LINQ
- **No LINQ chains in per-request code** — each `Where/Select/OrderBy` allocates an enumerator + closure + delegate; `ToList()/ToArray()`/`params` allocate arrays. Use `for`/`foreach`. LINQ is fine in startup, DI wiring, and tests.
- **Pre-size** any collection whose count you can estimate: `new List<T>(n)`, `new Dictionary<K,V>(n)`, `new StringBuilder(n)` — skips the double-and-copy growth cycle (~30 % faster for lists, ~50 %+ for dictionaries/sets). Never `ToList()/ToArray()` just to iterate; use the `Count`/`Length` property, never `Count()` on a materialized collection.
- Fixed lookup sets: `static readonly FrozenDictionary`/`FrozenSet` (existing pattern in tools).
### 2.5 Objects / async
- Cache reusables as `static readonly`: `JsonSerializerOptions`, compiled delegates, `FrozenSet`. `HttpClient` only via `IHttpClientFactory`.
- **Never** sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) — thread-pool starvation. Accept and propagate `CancellationToken` end-to-end from `HttpContext.RequestAborted`.
- `ValueTask` only for APIs proven mostly-synchronous; await it exactly once.
### 2.6 Regex & reflection — precompile or cache; never build them per request
Both spin up expensive runtime machinery (pattern parse + compile/interpret; metadata walking + boxing + JIT) and allocate. Building either on the request path pays that cost on every call. Rule: move the work to compile time, or do it once and cache.
 
**Regex — prefer plain string methods; when you truly need it, source-generate it**
- For a fixed prefix/suffix/contains/equality/split check, a `string`/`ReadOnlySpan<char>` method (`StartsWith`, `Contains`, `IndexOf`, `Split`) is dramatically faster and clearer than `Regex` — no pattern parse, no `Match` allocation. Reach for regex only for genuinely complex patterns whose hand-written equivalent would be verbose.
- A compile-time-known pattern MUST use `[GeneratedRegex]` on a `partial` method (SYSLIB1045), never `new Regex(pattern)` or static `Regex.IsMatch(input, pattern)` per call. The source generator caches a singleton `Regex`, precompiles the pattern (throughput of `RegexOptions.Compiled` with none of the runtime startup cost), and is trim/AOT-friendly. `RegexOptions.Compiled` is redundant with it — don't add it.
```csharp
// ❌ parses + compiles the pattern on every call, allocates a Match
if (Regex.IsMatch(cpf, @"^\d{11}$")) ...
// ✅ source-generated: cached singleton, zero per-call setup
[GeneratedRegex(@"^\d{11}$")]
private static partial Regex CpfDigits();
if (CpfDigits().IsMatch(cpf)) ...
// ✅ better still here — no regex at all
if (cpf.Length == 11 && cpf.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0) ...
```
- **Security (priority 1):** regex over **untrusted input** without a timeout is a ReDoS denial-of-service — catastrophic backtracking can peg a thread for *hours* on adversarial input. Always pass a `matchTimeout` (the default is `Regex.InfiniteMatchTimeout` — no limit), or use `RegexOptions.NonBacktracking` (.NET 7+) for a linear-time guarantee. Patterns must be **developer-authored** — never assemble a pattern from user input; timeouts/NonBacktracking are not a boundary against hostile *patterns*. Validate untrusted input by length/range/whitelist first (§4).
**Reflection — resolve once, cache the result; prefer generics / source generators**
- **Never** call `GetType().GetProperty/GetProperties/GetMethod/GetCustomAttributes` or `Activator.CreateInstance` per request. Lookups walk metadata and allocate; `MethodInfo.Invoke` boxes every argument and is far slower than a direct call.
- If reflection is unavoidable, do it **once at startup / first use** and cache into a `static readonly` field or `FrozenDictionary<Type, …>` (e.g. a handler→`ToolName` resolution belongs in a per-type cache, not recomputed each invocation). For repeated invocation, compile a delegate (`Delegate.CreateDelegate` / a cached compiled expression) instead of `MethodInfo.Invoke`.
- Prefer the mechanisms that remove reflection entirely:
  - Type creation: generic `T where T : new()` instead of `Activator.CreateInstance(type)`.
  - Serialization: cached `static readonly JsonSerializerOptions`; for a proven hot-path, a source-generated `JsonSerializerContext` removes runtime reflection and is trim/AOT-safe.
  - Trusted private-member access: `[UnsafeAccessor]` (.NET 8+) — a direct accessor with no lookup.
- Reflection also defeats trimming/Native AOT (`RequiresUnreferencedCode`/`RequiresDynamicCode` warnings) — another reason to keep it off the request path.
### 2.7 Large Object Heap (LOH) — avoid OOM and memory bloat
Any single allocation **≥ 85,000 bytes** — a `byte[]`, `char[]`, big `string`, or a `List<T>`/`StringBuilder` backing array that grew past it — goes to the **LOH**. The LOH is collected only by an expensive gen2 (full) GC and, by default, is **never compacted**: freed slots stay as holes. Repeated large alloc/free therefore **fragments** the heap until no hole is big enough — the heap keeps expanding and the process throws `OutOfMemoryException` (or is OOM-killed by its container) even though "free" memory exists. This is the classic slow leak of a high-throughput service. Keep large transient objects **off** the LOH:
 
- **Don't materialize large blobs.** Never read a whole request/response body, file, or query result into one `byte[]`/`string`. **Stream** it: `JsonSerializer.DeserializeAsync(stream, …)` from the `Stream`/`PipeReader`, serialize into the response via `Utf8JsonWriter`, process in chunks. ASP.NET Core already streams over pooled buffers — write to the body, don't buffer beside it.
- **Pool large buffers instead of allocating them.** `ArrayPool<T>.Shared.Rent(n)` + `Return` in `try/finally` for transient large arrays (LOH offenders are almost always byte/char buffers). For streams, use **`Microsoft.IO.RecyclableMemoryStream`** instead of `new MemoryStream()` wherever payloads can be large — it chains small pooled buffers, so the stream never touches the LOH and never fragments.
- **Pre-size to avoid growth churn.** A `List<T>`/`Dictionary`/`StringBuilder` that doubles its backing array emits large intermediate arrays straight onto the LOH. Set an initial capacity (§2.4) so it can't cross 85 KB by accident; if the data is inherently huge, **segment it** (many small arrays) rather than one contiguous block.
- **Strings count too:** a string ≥ ~42,500 chars is on the LOH. Don't build giant strings in memory (huge JSON, CSV exports, concatenated payloads) — stream them out.
- **Compaction is a last resort, never a hot-path tool.** After a known one-off burst, a single `GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;` reclaims fragmentation on the next gen2 GC — but it's a blocking, stop-the-world pass. Fix the allocation pattern first; `System.GC.ConserveMemory` (1–9) can auto-compact under fragmentation if you genuinely cannot.
- **Diagnose before acting:** confirm the LOH is the culprit with `dotnet-counters` (gen-2/LOH size, `% Time in GC`) and `dotnet-gcdump` — don't guess.
### 2.8 Memory leaks — reachability & lifetime
A managed "leak" in .NET is not unfreed memory — it's an object kept **reachable** longer than intended, so the GC never collects it. §2.1–§2.7 fight *transient* garbage; this fights *permanent rooting*. In a long-lived host (singletons, `static`, the DI container) the roots below grow silently until the process OOMs — the "memory climbs and never comes back" symptom.
 
- **Every `+=` needs a `-=` — the #1 managed leak.** While a publisher holds your handler it holds *you*: the subscriber can't be collected. A `static` event, or a **singleton / long-lived publisher, roots the subscriber for the app's lifetime.** A type that subscribes to an external event MUST implement `IDisposable` and unsubscribe in `Dispose`, removing the *same* delegate instance it added.
```csharp
// ❌ subscriber leaks — long-lived publisher, never unsubscribed
_ticker.Elapsed += OnTick;
// ✅ subscriber owns its subscription and releases it
public void Dispose() => _ticker.Elapsed -= OnTick;
```
  Better: avoid C# `event`s across DI lifetime boundaries — inject an interface, an `IHostedService`, or a `Channel<T>` instead.
- **`static` mutable state is a GC root for the whole process.** Never accumulate into a `static` field/collection/event — it is never collected. Keep statics `static readonly` and immutable; never use one for growing data.
- **Every cache is bounded, or it is a leak.** A `Dictionary`/`ConcurrentDictionary` used as a cache that only ever `Add`s grows forever. Use `IMemoryCache` with a **`SizeLimit` + expiration** (or an explicit LRU/TTL with eviction). A "cache" with no eviction policy is unbounded growth by definition.
```csharp
// ❌ unbounded — only Adds, grows for the life of the process
private static readonly ConcurrentDictionary<string, Quote> _cache = new();
// ✅ bounded: size cap + TTL so entries evict
services.AddMemoryCache(o => o.SizeLimit = 10_000);            // registration
_memoryCache.Set(key, quote, new MemoryCacheEntryOptions {
    Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
```
- **Dispose everything you own.** `using` / `await using` for any `IDisposable`/`IAsyncDisposable` — streams, DB connections, `RecyclableMemoryStream`, `SemaphoreSlim`, `CancellationTokenSource`. A type holding disposables is itself `IDisposable`/`IAsyncDisposable` and disposes them. "Who creates it, disposes it" — but never dispose what DI owns (scoped/singleton services).
- **Timers & cancellation are silent leaks.** A running `System.Threading.Timer`/`Timers.Timer` roots its callback target — `Dispose` it. `CancellationTokenSource` must be disposed (it can hold a timer via `CancelAfter`); and `token.Register(...)` on a **long-lived** token returns a `CancellationTokenRegistration` you must dispose, or the callbacks pile up.
- **Long-lived delegates/closures root everything they capture.** A cached delegate, a registered callback, or a captured `this` keeps the whole captured graph alive — capture the minimum, or use `static` lambdas (§2.2). For an identity-keyed cache that must not keep its keys alive, use `ConditionalWeakTable<,>` / `WeakReference<T>`.
- **Diagnose:** a heap that climbs across GCs and never drops is a leak, not pressure — diff two `dotnet-gcdump` snapshots for the growing type; `dotnet-counters` gen-2/`gc-heap-size` trending up confirms it.
## 3. Concurrency & thread-safety — the hot path is multi-threaded
 
Thousands of requests run concurrently on the thread pool; any state shared across requests is touched by many threads at once. Correctness here is non-negotiable — a data race is a heisenbug that ships. Order of defenses: **① no shared mutable state ② share only immutable snapshots ③ lock-free atomics (`Interlocked`) ④ `lock` for compound invariants.** `volatile` is a last resort. Note: reads/writes of references and ≤32-bit primitives are individually atomic (no torn values), but are **not** ordered or guaranteed-visible across threads without one of the barriers below.
 
### 3.1 Default: don't share mutable state
- Handlers, validators, tools, and adapters are resolved **per request** (scoped/transient) — keep them **stateless**. Never add a mutable instance field to a handler, or to a singleton, to stash per-request data: a singleton field is shared by every concurrent request.
- Share only **immutable** data (`readonly record`, `Frozen*` collections) — immutable is automatically thread-safe.
- To refresh a shared cache/config, **build a new immutable snapshot and publish it atomically** (`Interlocked.Exchange`/`CompareExchange`), never mutate in place — e.g. the tenant-config warmup/refresh cache.
- Ambient per-request context flows through `AsyncLocal<T>` — **never** `[ThreadStatic]`/`ThreadLocal<T>`: async continuations hop threads, so thread-local state leaks across requests or vanishes mid-flow.
### 3.2 `Interlocked` — lock-free atomics (first choice for simple shared state)
Atomic and far cheaper than `lock` (no kernel transition, no blocking) — the right tool for counters, flags, accumulators, and single-field publish in the hot path.
```csharp
// ❌ read-modify-write: two threads can lose an update
_count++;
// ✅ atomic
Interlocked.Increment(ref _count);
 
// ✅ lock-free snapshot publish — build fully, then swap in one op
var next = BuildSnapshot(...);
Interlocked.Exchange(ref _snapshot, next);
```
- Methods: `Increment`/`Decrement`, `Add`, `Exchange` (set + return old), `CompareExchange(ref x, newVal, expected)` (the CAS that lock-free algorithms build on), `Read(ref long)` (atomic 64-bit read on 32-bit platforms).
- Making each of several fields atomic does **not** make a multi-field operation atomic — that needs a single CAS on an immutable snapshot, or `lock` (§3.4).
- For metrics, prefer the thread-safe OTel `Meter` instruments (§5) over hand-rolled counters.
### 3.3 `volatile` — visibility only, last resort
- `volatile` / `Volatile.Read` / `Volatile.Write` guarantee **visibility + ordering** of a single field, **not atomicity**. Microsoft's own guidance: prefer `Interlocked`, `lock`, or the `Volatile` class over the `volatile` keyword.
- Only defensible use: one writer, many readers, no read-modify-write (e.g. a `bool _stopRequested` flag) — and even then `Interlocked` is usually clearer.
- ❌ Illegal on `long`/`ulong`/`double`/`decimal` (>32-bit, not atomic). ❌ Does not make `x++` or `balance -= n` safe (still a race). ❌ Never a lock substitute.
- When you do need it, prefer `Volatile.Read(ref f)`/`Volatile.Write(ref f, v)` at the access site over marking the whole field `volatile` — scoped, explicit intent.
### 3.4 `lock` — mutual exclusion for compound invariants
Use when several fields must stay consistent or a critical section is multi-step; `Monitor.Enter`/`Exit` insert the barriers so writes inside are visible to the next holder.
```csharp
private readonly object _gate = new();     // dedicated private lock object
lock (_gate) { /* smallest possible critical section */ }
```
- **Never** `lock(this)`, `lock(typeof(T))`, or `lock("literal")` — external/runtime code can lock the same object and deadlock. Use a `private readonly object` (or `System.Threading.Lock` on .NET 9+).
- **Never `await` inside a `lock`.** For async mutual exclusion use `SemaphoreSlim(1, 1)` with `WaitAsync`/`Release` in `try/finally`.
- Keep it minimal — no I/O, no `await`, no calls into unknown code. Lock contention stalls threads and wrecks hot-path throughput; if you're reaching for a lock in the hot path, first check whether an immutable snapshot + `Interlocked` removes the need.
### 3.5 Prefer ready-made thread-safe primitives over hand-rolled sync
- Read-mostly shared map/cache → `ConcurrentDictionary<K,V>`. But `GetOrAdd`/`AddOrUpdate` value factories are **not** atomic (may run more than once under contention): keep the factory side-effect-free, or store `Lazy<T>` values for exactly-once init.
- Lazy singleton / expensive one-time init → `Lazy<T>` (thread-safe by default). Do **not** hand-write double-checked locking; if you ever must, the backing field must be `volatile` or a partially-constructed instance can leak to another thread.
- Producer/consumer, buffering, backpressure → `System.Collections.Concurrent` (`ConcurrentQueue`, `Channel<T>`); read-heavy shared state → `ReaderWriterLockSlim`.
- Don't rely on the strong x86/x64 hardware ordering — .NET only guarantees the (weaker) ECMA memory model, and ARM64 hardware is weaker too. Correctness must come from these primitives, not from "it worked on my machine."
## 4. Security — priority 1, overrides everything
 
- PII (CPF, phone, name, tokens, document ids, full request/response bodies) NEVER goes to: logs, exception messages, metric tags, span names/tags/baggage, test fixtures. Mask or omit.
- Validate + whitelist ALL external input at the boundary (schema, type, length, range); reject — never sanitize-and-continue.
- Secrets only via deploy placeholders (`#{...}#` Octopus); never committed, never logged, never in defaults.
- Security-sensitive randomness: `RandomNumberGenerator`, not `Random`. Secret comparison: `CryptographicOperations.FixedTimeEquals`.
- User-facing errors are generic and user-safe; no stack traces or internals.
## 5. Telemetry — only what is consumed
 
Logs (Serilog):
- Message templates only (`"... {Prop}"`, `{@Obj}` sparingly); NEVER interpolation/concat in log calls.
- Log decision points and failures, not play-by-play. Max 2 consecutive log calls (S6664). NEVER log per iteration in hot path.
- Levels: `Information` = queryable state change; `Warning` = recoverable anomaly; `Error` = failed operation.
Metrics (OTel `Meter`):
- Create a metric ONLY if a dashboard or alert will consume it. Follow the existing pattern: static class in `*/Telemetry/`, snake_case `blip_cred_*` name, low-cardinality tags only (`tenant_id`/`tool`/`topic`-like — NEVER user ids), `Add(0)` warm-up initializer, `AddMeter` registration, MeterListener test.
Traces (W3C Trace Context):
- Use `Activity`/`ActivitySource` — never invent correlation ids; `traceparent` propagates to all outbound calls (HttpClient automatic; Lime via `LimeEnvelopeTracing`).
- Span names low-cardinality (`MCP {tool}`, `HTTP {method} {host}`) — never full URLs or user data. Enrich via `Activity.Current?.SetTag/AddEvent`; on failure set `ActivityStatusCode.Error` and record exception type, not payloads.
- Baggage propagates via HTTP headers — NEVER put PII/secrets in baggage.
## 6. Tests — ≥90% on new/changed code
 
- Every new/changed class ships unit tests in the same PR: ≥90% line coverage of the new/changed code (repo-wide floor remains 80%), covering success, failure/`Fail` result, and null/edge paths.
- Isolated: no network/disk/external services. NSubstitute for deps, Shouldly asserts; metrics via MeterListener, spans via ActivityListener (existing patterns).
- A bugfix starts with a failing test reproducing it.
## 7. Style — priority 3, never traded against 1–2
 
- NO comments inside method bodies, except a single line stating a non-obvious constraint (the WHY). Never narrate WHAT code does. No commented-out code. No TODO without a work item.
- Public members get `<summary>`. Guard clauses over nesting; cognitive complexity ≤15 (S3776); ≤7 ctor params (S107); string repeated 3+ times becomes a constant.
- Match surrounding code (naming, layout, patterns) and run `dotnet format` before commit.
 