using FFmpegWrapper;
using System.Collections.Concurrent;


int MaxConcurrent = 1; // Maximum number of files concurrently scanned.
SemaphoreSlim SL = new SemaphoreSlim(MaxConcurrent);
ConcurrentBag<Task> TasksCache = new ConcurrentBag<Task>();

/* A quick and easy take on an FFmpeg wrapper. Supports lightweight threading options and thread-safe concurrency
 * For larger operations, like batching. Use as is or create your own commands for the process. */
void UnitTesting()
{
    QueueFiles(new string[] { "filePath" });
    Console.ReadLine();
}

async void QueueFiles(string[] Files)
{
    foreach (string File in Files)
    {
        await SL.WaitAsync();
        TasksCache.Add(ProcessFile(File, SL));
    }
    await Task.WhenAll(TasksCache);
    // All files have been processed.
}

async Task ProcessFile(string FilePath, SemaphoreSlim SL)
{
    try
    {
        if (FilePath == null || !File.Exists(FilePath)) return;
        var ffmpegWrapper = new Wrapper(@"ffprobePath");
        var cancellationTokenSource = new CancellationTokenSource();
        var attributes = await ffmpegWrapper.GetVideoStreamXML(FilePath, cancellationTokenSource.Token);

        Console.WriteLine($"Task Result ffprobWrapper for: {Path.GetFileName(FilePath)}\n\n{attributes}");
    }
    finally
    {
        SL.Release();

    }
}

UnitTesting();



