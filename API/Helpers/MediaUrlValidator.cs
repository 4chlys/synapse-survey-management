using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Helpers;

public static class MediaUrlValidator
{
    private static readonly HashSet<string> YoutubeHosts = new(StringComparer.OrdinalIgnoreCase)
        { "youtube.com", "www.youtube.com", "youtu.be" };

    private static readonly HashSet<string> VimeoHosts = new(StringComparer.OrdinalIgnoreCase)
        { "vimeo.com", "www.vimeo.com", "player.vimeo.com" };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".m4a", ".wav", ".ogg" };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".webm" };

    // Maps MediaType to the storage subfolder name
    private static string TypeFolder(QuestionMediaType type) => type switch
    {
        QuestionMediaType.Image => "images",
        QuestionMediaType.Video => "videos",
        QuestionMediaType.Audio => "audios",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>
    /// Returns (true, canonicalUrl, null) on success or (false, null, errorMessage) on failure.
    /// canonicalUrl equals url for uploaded files; YouTube/Vimeo embed URLs are normalized.
    /// </summary>
    public static (bool Valid, string CanonicalUrl, string Error) Validate(
        string url, QuestionMediaType? type, bool isEmbed,
        string gcsBucket, bool isDevelopment)
    {
        // null type — url must be absent (MediaText alone is allowed)
        if (type is null)
        {
            if (!string.IsNullOrEmpty(url))
                return (false, null, "MediaUrl must be empty when MediaType is not set.");
            return (true, null, null);
        }

        if (string.IsNullOrEmpty(url))
            return (false, null, "MediaUrl is required when MediaType is set.");

        if (!isEmbed)
            return ValidateUploadedUrl(url, type.Value, gcsBucket, isDevelopment);

        return ValidateEmbedUrl(url, type.Value);
    }

    private static (bool Valid, string CanonicalUrl, string Error) ValidateUploadedUrl(
        string url, QuestionMediaType type, string gcsBucket, bool isDevelopment)
    {
        var folder = TypeFolder(type);

        if (isDevelopment)
        {
            // Local shape: {scheme}://{host}/uploads/questions/{folder}/...
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return (false, null, "MediaUrl is not a valid absolute URL.");
            var expectedPath = $"/uploads/questions/{folder}/";
            if (!uri.AbsolutePath.StartsWith(expectedPath, StringComparison.OrdinalIgnoreCase))
                return (false, null, $"Uploaded {type} URL must reference the questions/{folder}/ storage path.");
            return (true, url, null);
        }

        // GCS shape: https://storage.googleapis.com/{bucket}/questions/{folder}/...
        var expectedPrefix = $"https://storage.googleapis.com/{gcsBucket}/questions/{folder}/";
        if (!url.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return (false, null, $"Uploaded {type} URL must reference the GCS questions/{folder}/ path.");
        return (true, url, null);
    }

    private static (bool Valid, string CanonicalUrl, string Error) ValidateEmbedUrl(
        string url, QuestionMediaType type)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, null, "MediaUrl is not a valid absolute URL.");

        if (uri.Scheme != "https")
            return (false, null, "URL must use HTTPS.");

        if (type == QuestionMediaType.Image)
            return (true, url, null); // any HTTPS URL is accepted for images

        if (type == QuestionMediaType.Video)
        {
            // YouTube / Vimeo: canonicalize to embed URL
            if (YoutubeHosts.Contains(uri.Host) || VimeoHosts.Contains(uri.Host))
                return ValidateVideoEmbedUrl(uri);
            // Direct video file URL
            if (VideoExtensions.Contains(Path.GetExtension(uri.AbsolutePath)))
                return (true, url, null);
            return (false, null, "Video URL must be a YouTube/Vimeo link or a direct .mp4/.webm URL.");
        }

        // Audio: path ends in known audio extension
        if (!AudioExtensions.Contains(Path.GetExtension(uri.AbsolutePath)))
            return (false, null, "Audio URL must end with .mp3, .m4a, .wav, or .ogg.");
        return (true, url, null);
    }

    private static (bool Valid, string CanonicalUrl, string Error) ValidateVideoEmbedUrl(Uri uri)
    {
        var host = uri.Host;

        if (YoutubeHosts.Contains(host))
        {
            var id = ExtractYoutubeId(uri);
            if (id is null)
                return (false, null, "Could not extract a YouTube video ID from the URL.");
            return (true, $"https://www.youtube.com/embed/{id}", null);
        }

        if (VimeoHosts.Contains(host))
        {
            var id = ExtractVimeoId(uri);
            if (id is null)
                return (false, null, "Could not extract a Vimeo video ID from the URL.");
            return (true, $"https://player.vimeo.com/video/{id}", null);
        }

        return (false, null, "Video embed URL must be a YouTube or Vimeo link.");
    }

    private static string ExtractYoutubeId(Uri uri)
    {
        // youtu.be/{id}
        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var seg = uri.AbsolutePath.TrimStart('/');
            return string.IsNullOrEmpty(seg) ? null : seg.Split('/')[0];
        }

        // /embed/{id} (already canonical)
        if (uri.AbsolutePath.StartsWith("/embed/", StringComparison.OrdinalIgnoreCase))
        {
            var seg = uri.AbsolutePath["/embed/".Length..].Split('/')[0];
            return string.IsNullOrEmpty(seg) ? null : seg;
        }

        // ?v={id}
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var v = query["v"];
        return string.IsNullOrEmpty(v) ? null : v;
    }

    private static string ExtractVimeoId(Uri uri)
    {
        // player.vimeo.com/video/{id} or vimeo.com/{id}
        var parts = uri.AbsolutePath.TrimStart('/').Split('/');
        foreach (var part in parts)
        {
            if (long.TryParse(part, out _))
                return part;
        }
        return null;
    }
}
