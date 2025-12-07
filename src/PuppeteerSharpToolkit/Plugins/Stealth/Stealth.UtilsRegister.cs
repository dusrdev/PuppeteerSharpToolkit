using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;

using PuppeteerSharp;

namespace PuppeteerSharpToolkit.Plugins;

public static partial class Stealth {
    /// <summary>
    /// Idempotent-ly registers utils.js (will not throw exception if it already exists)
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    public static Task RegisterUtilsAsync(IPage page) {
        return UtilsRegister.RegisterAsync(page);
    }

    private static class UtilsRegister {
        private static readonly ConditionalWeakTable<IPage, SemaphoreSlim> Locks = [];
        private static readonly ConditionalWeakTable<IPage, RegisteredState> Register = [];
        private sealed class RegisteredState { public bool BackfillDone; }

        internal static async Task RegisterAsync(IPage page) {
            var @lock = Locks.GetValue(page, _ => new SemaphoreSlim(1, 1));
            await @lock.WaitAsync().ConfigureAwait(false);
            try {
                // Ensure all future documents/frames get utils.js automatically
                await page.EvaluateExpressionOnNewDocumentAsync(Scripts.Utils).ConfigureAwait(false);

                var state = Register.GetOrCreateValue(page);
                if (!state.BackfillDone) {
                    // Backfill current frames once
                    var count = page.Frames.Length;
                    var buffer = ArrayPool<IFrame>.Shared.Rent(count);
                    try {
                        page.Frames.CopyTo(buffer, 0);
                        var frames = new ArraySegment<IFrame>(buffer, 0, count);
                        await Task.WhenAll(frames.Select(EnsureUtilsInFrameAsync)).ConfigureAwait(false);
                    } finally {
                        ArrayPool<IFrame>.Shared.Return(buffer);
                    }
                    state.BackfillDone = true;
                }
            } finally {
                @lock.Release();
            }
        }

        private static async Task EnsureUtilsInFrameAsync(IFrame frame) {
            // If the frame is already dead or about to swap, existence check can fail; we still try to inject with retry.
            bool exists = false;
            try {
                exists = await UtilsExistsAsync(frame).ConfigureAwait(false);
            } catch (Exception ex) when (IsTransientExecutionContext(ex)) {
                // Ignore and proceed to injection with retries.
            }

            if (exists) {
                return;
            }

            await InjectWithRetryAsync(frame, Scripts.Utils).ConfigureAwait(false);

            // Returns true if utils is already present in the given frame.
            static Task<bool> UtilsExistsAsync(IFrame frame) {
                // Keep the existence probe very small and side-effect free.
                const string probe = @"() => {
                const g = (typeof globalThis !== 'undefined') ? globalThis : (typeof window !== 'undefined' ? window : this);
                return typeof g.utils !== 'undefined';
            }";
                return frame.EvaluateFunctionAsync<bool>(probe);
            }

            // Checks for common transient errors during navigation/context churn.
            static bool IsTransientExecutionContext(Exception ex) {
                if (ex is not PuppeteerException puppeteerException || puppeteerException.Message is null) {
                    return false;
                }
                var message = puppeteerException.Message;
                return message.IndexOf("Execution context was destroyed", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("Cannot find context with specified id", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("Target closed", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            static async Task InjectWithRetryAsync(IFrame frame, string script, int attempts = 5, int initialDelayMs = 25) {
                Exception? last = null;
                var rng = Random.Shared;

                for (int i = 1; i <= attempts; i++) {
                    try {
                        await frame.EvaluateExpressionAsync(script).ConfigureAwait(false);
                        return;
                    } catch (Exception ex) when (IsTransientExecutionContext(ex) && i < attempts) {
                        last = ex;
                        // Exponential backoff with small jitter.
                        var backoff = initialDelayMs * (int)Math.Pow(2, i - 1);
                        await Task.Delay(backoff + rng.Next(0, 15)).ConfigureAwait(false);
                        continue;
                    } catch {
                        // Non-transient error: fail fast.
                        throw;
                    }
                }

                // All attempts hit transient errors; surface the last one.
                throw last ?? new InvalidOperationException("Failed to inject utils after retries.");
            }
        }
    }
}
