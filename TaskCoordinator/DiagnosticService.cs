
using System.Diagnostics;

namespace TaskCoordinator;

public sealed class DiagnosticService
{
    public async Task<DiagnosticResult> RunAsync(
        string name,
        TimeSpan duration,
        CancellationToken token = default)
    {
        // TODO 1: Проверка аргументов
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя диагностики не может быть пустым.", nameof(name));

        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        token.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        // TODO 2: Передача token в Task.Delay
        await Task.Delay(duration, token);

        sw.Stop();

        // TODO 3: Возвращение результата
        return new DiagnosticResult(
            name,
            DiagnosticStatus.Completed,
            "Диагностика успешно завершена.",
            sw.Elapsed);
    }

    public async Task<DiagnosticResult> RunCpuBoundAsync(
        string name,
        int iterations,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя не может быть пустым.", nameof(name));

        if (iterations < 0)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        // TODO 4: CPU-bound работа переносится в Task.Run
        return await Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();
            long sum = 0;

            for (int i = 0; i < iterations; i++)
            {
                if (i % 1000 == 0)
                    token.ThrowIfCancellationRequested();

                sum += i % 7;
            }

            token.ThrowIfCancellationRequested();

            sw.Stop();

            return new DiagnosticResult(
                name,
                DiagnosticStatus.Completed,
                $"Сумма: {sum}",
                sw.Elapsed);

        }, token);
    }

    public async Task<IReadOnlyList<DiagnosticResult>> RunAllAsync(
        CancellationToken token = default)
    {
        var tasks = new[]
        {
            RunAsync("Сеть", TimeSpan.FromMilliseconds(600), token),
            RunAsync("Диск", TimeSpan.FromMilliseconds(900), token),
            RunAsync("База", TimeSpan.FromMilliseconds(700), token)
        };

        // TODO 5: Ожидание завершения всех задач
        return await Task.WhenAll(tasks);
    }

    public async Task<DiagnosticResult> RunWithTimeoutAsync(
        string name,
        TimeSpan duration,
        TimeSpan timeout,
        CancellationToken token = default)
    {
        if (timeout < TimeSpan.Zero &&
            timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        // TODO 6: Создание linked CancellationTokenSource
        using var cts =
            CancellationTokenSource.CreateLinkedTokenSource(token);

        // TODO 7: Настройка тайм-аута
        cts.CancelAfter(timeout);

        try
        {
            return await RunAsync(name, duration, cts.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            // TODO 8: Отличаем тайм-аут от внешней отмены
            throw new TimeoutException(
                $"Диагностика '{name}' превысила время ожидания {timeout}.");
        }
    }

    public async Task<string> FirstFinishedAsync(
        CancellationToken token = default)
    {
        var tasks = new[]
        {
            RunAsync("Быстрая", TimeSpan.FromMilliseconds(150), token),
            RunAsync("Средняя", TimeSpan.FromMilliseconds(350), token),
            RunAsync("Медленная", TimeSpan.FromMilliseconds(600), token)
        };

        // TODO 9: Ожидание первой завершившейся задачи
        var firstTask = await Task.WhenAny(tasks);

        var result = await firstTask;

        return result.Name;
    }

    // TODO 10: Обертка для обработки ошибок
    public async Task<DiagnosticResult> RunSafeAsync(
        string name,
        TimeSpan duration,
        CancellationToken token = default)
    {
        try
        {
            return await RunAsync(name, duration, token);
        }
        catch (OperationCanceledException ex)
        {
            return new DiagnosticResult(
                name,
                DiagnosticStatus.Cancelled,
                ex.Message,
                TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            return new DiagnosticResult(
                name,
                DiagnosticStatus.Failed,
                ex.Message,
                TimeSpan.Zero);
        }
    }
}
