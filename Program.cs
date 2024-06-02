using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("bad-download/v1", async (CancellationToken cancellationToken) =>
{
    using var httpClient = new HttpClient();

    httpClient.Timeout = TimeSpan.FromMinutes(10);

    var startTimeStamp = Stopwatch.GetTimestamp();

    var response = await httpClient.GetAsync("https://freetestdata.com/wp-content/uploads/2024/05/Ftd-20MB.mpeg", cancellationToken);

    response.EnsureSuccessStatusCode();

    var contentLength = response.Content.Headers.ContentLength;

    var contentType = response.Content.Headers.ContentType;

    using var body = await response.Content.ReadAsStreamAsync(cancellationToken);

    var folderPath = $"wwwroot/files";

    var directory = Directory.CreateDirectory(folderPath);

    using (var fileStream = new FileStream($"{directory.FullName}/{Guid.NewGuid()}.mpeg", FileMode.Create, FileAccess.Write))
    {
        body.CopyTo(fileStream);
    }

    var elapsedTime = Stopwatch.GetElapsedTime(startTimeStamp);

    return new FileData(contentLength, contentType?.MediaType ?? string.Empty, elapsedTime.TotalMilliseconds);
});

app.MapGet("bad-download/v2", async (CancellationToken cancellationToken) =>
{
    using var httpClient = new HttpClient();

    httpClient.Timeout = TimeSpan.FromMinutes(10);

    var startTimeStamp = Stopwatch.GetTimestamp();

    var body = await httpClient.GetByteArrayAsync("https://freetestdata.com/wp-content/uploads/2024/05/Ftd-20MB.mpeg", cancellationToken);

    var contentLength = body.Length;

    var folderPath = $"wwwroot/files";

    var directory = Directory.CreateDirectory(folderPath);

    using (var fileStream = new FileStream($"{directory.FullName}/{Guid.NewGuid()}.mpeg", FileMode.Create, FileAccess.Write))
    {
        await fileStream.WriteAsync(body.AsMemory(0, contentLength), cancellationToken);
    }

    var elapsedTime = Stopwatch.GetElapsedTime(startTimeStamp);

    return new FileData(contentLength, string.Empty, elapsedTime.TotalMilliseconds);
});

app.MapGet("good-download", async (CancellationToken cancellationToken) =>
{
    using var httpClient = new HttpClient();

    httpClient.Timeout = TimeSpan.FromMinutes(10);

    var startTimeStamp = Stopwatch.GetTimestamp();

    var response = await httpClient.GetAsync("https://freetestdata.com/wp-content/uploads/2024/05/Ftd-20MB.mpeg", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    response.EnsureSuccessStatusCode();

    var contentLength = response.Content.Headers.ContentLength;

    var contentType = response.Content.Headers.ContentType;

    var folderPath = $"wwwroot/files";

    var directory = Directory.CreateDirectory(folderPath);

    using var body = await response.Content.ReadAsStreamAsync(cancellationToken);

    using var fileStream = new FileStream($"{directory.FullName}/{Guid.NewGuid()}.mpeg", FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

    byte[] buffer = new byte[8192];

    int bytesRead;

    while ((bytesRead = await body.ReadAsync(buffer, cancellationToken)) > 0)
    {
        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
    }

    var elapsedTime = Stopwatch.GetElapsedTime(startTimeStamp);

    return new FileData(contentLength, contentType?.MediaType ?? string.Empty, elapsedTime.TotalMilliseconds);
});

app.Run();

internal record FileData(long? FileLenght, string ContentType, double DownloadDuration);