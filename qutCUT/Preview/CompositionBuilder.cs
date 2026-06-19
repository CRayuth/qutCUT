using System.Text;
using qutCUT.Models;
using qutCUT.Utilities;
using Xabe.FFmpeg;

namespace qutCUT.Preview;

// Replaces AVFoundation CompositionBuilder — builds an FFmpeg filtergraph from a Timeline.
public sealed class CompositionBuilder : IDisposable
{
    public async Task BuildAsync(
        Timeline timeline,
        string outputPath,
        ExportPreset preset,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var videoTracks = timeline.Tracks.Where(t => t.Type == TrackType.Video && !t.Hidden).ToList();
        var audioTracks = timeline.Tracks.Where(t => t.Type == TrackType.Audio && !t.Muted).ToList();

        var inputs     = new List<string>();
        var filterGraph = new StringBuilder();
        var videoLabels = new List<string>();
        var audioLabels = new List<string>();

        int inputIndex = 0;

        // Build per-clip inputs and filters
        foreach (var track in videoTracks)
        {
            foreach (var clip in track.Clips.OrderBy(c => c.StartFrame))
            {
                if (clip.MediaRef is null) continue;

                // Validate before interpolating into the argument string — prevents
                // a malicious filename from injecting FFmpeg options or protocols.
                string safeRef;
                try { safeRef = MediaPathValidator.ValidateExists(clip.MediaRef); }
                catch (Exception ex)
                {
                    Log.Preview.LogWarning("Skipping clip with unsafe MediaRef: {err}", ex.Message);
                    continue;
                }

                var trimStart = TimeFormatting.FramesToSeconds(clip.TrimStartFrame, timeline.Fps);
                var trimEnd   = TimeFormatting.FramesToSeconds(clip.TrimStartFrame + clip.DurationFrames, timeline.Fps);
                var pts       = TimeFormatting.FramesToSeconds(clip.StartFrame, timeline.Fps);

                inputs.Add($"-ss {trimStart:F4} -t {(trimEnd - trimStart):F4} -i \"{safeRef}\"");

                var label = $"v{inputIndex}";

                // Scale to canvas size
                filterGraph.Append($"[{inputIndex}:v]scale={preset.Width}:{preset.Height}:force_original_aspect_ratio=decrease,pad={preset.Width}:{preset.Height}:(ow-iw)/2:(oh-ih)/2");

                // Opacity
                if (clip.Opacity < 0.999)
                    filterGraph.Append($",format=rgba,colorchannelmixer=aa={clip.Opacity:F3}");

                // Transform (position, scale, rotation)
                if (clip.Transform.X != 0 || clip.Transform.Y != 0 || clip.Transform.ScaleX != 1 || clip.Transform.ScaleY != 1)
                {
                    var sx = clip.Transform.ScaleX;
                    var sy = clip.Transform.ScaleY;
                    filterGraph.Append($",scale=iw*{sx:F3}:ih*{sy:F3}");
                    if (clip.Transform.X != 0 || clip.Transform.Y != 0)
                    {
                        var ox = (int)(clip.Transform.X * preset.Width);
                        var oy = (int)(clip.Transform.Y * preset.Height);
                        filterGraph.Append($",pad={preset.Width}:{preset.Height}:{preset.Width / 2 + ox}:{preset.Height / 2 + oy}");
                    }
                }

                // PTS offset (place clip at correct timeline position)
                filterGraph.Append($",setpts=PTS+{pts:F4}/TB");
                filterGraph.Append($"[{label}];\n");

                videoLabels.Add(label);
                inputIndex++;
            }
        }

        // Audio tracks
        foreach (var track in audioTracks)
        {
            foreach (var clip in track.Clips.OrderBy(c => c.StartFrame))
            {
                if (clip.MediaRef is null) continue;

                string safeRef;
                try { safeRef = MediaPathValidator.ValidateExists(clip.MediaRef); }
                catch (Exception ex)
                {
                    Log.Preview.LogWarning("Skipping audio clip with unsafe MediaRef: {err}", ex.Message);
                    continue;
                }

                var trimStart = TimeFormatting.FramesToSeconds(clip.TrimStartFrame, timeline.Fps);
                var duration  = TimeFormatting.FramesToSeconds(clip.DurationFrames, timeline.Fps);
                var pts       = TimeFormatting.FramesToSeconds(clip.StartFrame, timeline.Fps);

                inputs.Add($"-ss {trimStart:F4} -t {duration:F4} -i \"{safeRef}\"");

                var label = $"a{inputIndex}";
                filterGraph.Append($"[{inputIndex}:a]volume={clip.Volume:F3},adelay={(int)(pts * 1000)}|{(int)(pts * 1000)}[{label}];\n");
                audioLabels.Add(label);
                inputIndex++;
            }
        }

        // Overlay all video layers
        string videoOut;
        if (videoLabels.Count == 0)
        {
            // Black canvas
            filterGraph.Append($"color=black:{preset.Width}x{preset.Height}:r={preset.Fps}[videoout];\n");
            videoOut = "[videoout]";
        }
        else if (videoLabels.Count == 1)
        {
            videoOut = $"[{videoLabels[0]}]";
        }
        else
        {
            // Stack overlays bottom to top
            var current = videoLabels[0];
            for (int i = 1; i < videoLabels.Count; i++)
            {
                var next = $"ov{i}";
                filterGraph.Append($"[{current}][{videoLabels[i]}]overlay=format=auto[{next}];\n");
                current = next;
            }
            videoOut = $"[{current}]";
        }

        // Mix all audio
        string audioOut;
        if (audioLabels.Count == 0)
        {
            filterGraph.Append("anullsrc=r=44100:cl=stereo[audioout];\n");
            audioOut = "[audioout]";
        }
        else if (audioLabels.Count == 1)
        {
            audioOut = $"[{audioLabels[0]}]";
        }
        else
        {
            var joinedLabels = string.Join("", audioLabels.Select(l => $"[{l}]"));
            filterGraph.Append($"{joinedLabels}amix=inputs={audioLabels.Count}:normalize=0[audioout];\n");
            audioOut = "[audioout]";
        }

        var totalDuration = TimeFormatting.FramesToSeconds(timeline.TotalFrames, timeline.Fps);
        var inputArgs     = string.Join(" ", inputs);
        var filter        = filterGraph.ToString().TrimEnd(';', '\n', ' ');

        // Output path is caller-controlled but validate anyway (no quotes, no protocols).
        var safeOutput = MediaPathValidator.Validate(outputPath);

        var ffmpegArgs = $"{inputArgs} -filter_complex \"{filter}\" -map \"{videoOut}\" -map \"{audioOut}\" " +
                         $"-c:v {preset.VideoCodec} -crf {preset.Crf} -preset {preset.Preset} " +
                         $"-c:a {preset.AudioCodec} -r {preset.Fps} -t {totalDuration:F4} " +
                         $"-y \"{safeOutput}\"";

        Log.Preview.LogDebug("FFmpeg args: {args}", ffmpegArgs);

        var conversion = FFmpeg.Conversions.New().AddParameter(ffmpegArgs);
        if (progress != null)
            conversion.OnProgress += (_, args) => progress.Report(args.Percent / 100.0);

        await conversion.Start(ct);
    }

    public void Dispose() { }
}
