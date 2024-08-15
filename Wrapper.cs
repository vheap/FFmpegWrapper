using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegWrapper
{
    public class Wrapper
    {
        private string ffprobePath;
        private string xmlArguments = "-v quiet -print_format json -show_format -show_streams -print_format xml";
        public Wrapper(string ffprobePath)
        {
            this.ffprobePath = ffprobePath;
        }

        private async Task<string> RunFFProbeCommandAsync(string arguments, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>();

            /* Setting up the parameters for the ffprobe/ffmpeg process. */
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            /* Making sure to queue the output for the threading. */
            var output = new ConcurrentQueue<string>();
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.Enqueue(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) output.Enqueue(args.Data); };
            process.Exited += (sender, args) => tcs.TrySetResult(string.Join(Environment.NewLine, output));

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (cancellationToken.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetCanceled();
                    try
                    {
                        process.Kill();
                    }
                    catch { /* Ignore exceptions if the process is already terminated */ }
                }
            }))
            {
                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    process.WaitForExit();
                    process.Dispose();
                }
            }
        }

        /* Validate video state. FFmpeg lacks the proper tools for it, so FFProbe is the only valid option. */
        public async Task<string> IsValidVideo(string videoPath, CancellationToken cancellationToken)
        {
            try
            {
                var output = await RunFFProbeCommandAsync($"-v error -i \"{videoPath}\"", cancellationToken);
                return output;
            }
            catch { }
            return string.Empty;
        }

        /* Extract and return the XML stream for the media file. */
        public async Task<string> GetVideoStreamXML(string videoPath, CancellationToken cancellationToken)
        {
            var output = await RunFFProbeCommandAsync($"\"{videoPath}\" {xmlArguments}", cancellationToken);
            return output;
        }

    }
}
