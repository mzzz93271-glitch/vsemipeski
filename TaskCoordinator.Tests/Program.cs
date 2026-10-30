
using TaskCoordinator;

var pass = 0;
var fail = 0;

void Check(bool ok, string n)
{
    if (ok)
    {
        pass++;
        Console.WriteLine("PASS " + n);
    }
    else
    {
        fail++;
        Console.WriteLine("FAIL " + n);
    }
}

var s = new DiagnosticService();


var one = await s.RunAsync("Сеть", TimeSpan.FromMilliseconds(20));

Check(
    one.Status == DiagnosticStatus.Completed,
    "одна диагностика завершается"
);


var all = await s.RunAllAsync();

Check(
    all.Count == 3 && all.All(x => x.Status == DiagnosticStatus.Completed),
    "WhenAll ждёт три диагностики"
);


var first = await s.FirstFinishedAsync();

Check(
    first == "Быстрая",
    "WhenAny возвращает первую завершившуюся"
);


using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(50);

    try
    {
        await s.RunAsync(
            "Отменяемая",
            TimeSpan.FromSeconds(5),
            cts.Token
        );

        Check(false, "кооперативная отмена");
    }
    catch (OperationCanceledException)
    {
        Check(true, "кооперативная отмена");
    }
}


try
{
    await s.RunWithTimeoutAsync(
        "Долгая диагностика",
        TimeSpan.FromSeconds(5),
        TimeSpan.FromMilliseconds(100)
    );

    Check(false, "тайм-аут вызывает TimeoutException");
}
catch (TimeoutException)
{
    Check(true, "тайм-аут вызывает TimeoutException");
}


try
{
    await s.RunAsync("", TimeSpan.FromMilliseconds(10));

    Check(false, "проверка неверного имени");
}
catch (ArgumentException)
{
    Check(true, "проверка неверного имени");
}


try
{
    await s.RunAsync(
        "Неверная диагностика",
        TimeSpan.FromSeconds(-1)
    );

    Check(false, "проверка отрицательной длительности");
}
catch (ArgumentOutOfRangeException)
{
    Check(true, "проверка отрицательной длительности");
}

Console.WriteLine(
    $"Итого: {pass} пройдено, {fail} ошибок."
);

return fail == 0 ? 0 : 1;
