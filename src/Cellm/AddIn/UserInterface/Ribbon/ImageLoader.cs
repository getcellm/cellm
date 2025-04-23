using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Svg;

namespace Cellm.AddIn.UserInterface.Ribbon;

public static class ImageLoader
{
    private static readonly ConcurrentDictionary<string, Bitmap> _imageCache =
        new ConcurrentDictionary<string, Bitmap>();

    public static Bitmap? LoadEmbeddedPngResized(string relativePath, int width = 16, int height = 16)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            // Handle invalid input path early
            Debug.WriteLine("Error: Relative path cannot be null or empty.");
            return null;
        }

        string cacheKey = relativePath.ToLowerInvariant();

        // 1. Check cache (TryGetValue handles null 'out' parameter correctly via bool return)
        if (_imageCache.TryGetValue(cacheKey, out var cachedBitmap))
        {
            // Important: Check if the cached item itself somehow became null (unlikely with TryAdd logic, but defensive)
            if (cachedBitmap != null)
            {
                return cachedBitmap; // Return cached version
            }
            else
            {
                // Remove the invalid null entry if it exists
                _imageCache.TryRemove(cacheKey, out _);
            }
        }

        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string resourceName = BuildResourceName(assembly, relativePath);

            // If BuildResourceName failed (e.g., due to assembly issues), it might return null or throw.
            // Let's handle null return explicitly here. BuildResourceName should ideally throw.
            if (resourceName == null)
            {
                // Error already logged in BuildResourceName or it threw an exception
                return null; // Or handle exception if BuildResourceName throws
            }

            // 2. Get resource stream (check for null return is already present)
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Debug.WriteLine($"Error: Embedded resource not found or stream is null. Attempted Name: {resourceName}");
                return null;
            }

            // Load the original bitmap from the stream
            using var originalBitmap = new Bitmap(stream);

            // Create the resized bitmap
            var resizedBitmap = new Bitmap(originalBitmap, new Size(width, height));

            // Add to cache. Use AddOrUpdate for slightly more robust caching logic.
            // The factory function ensures the bitmap is created only if needed.
            _imageCache.AddOrUpdate(cacheKey, resizedBitmap, (key, existing) =>
            {
                existing?.Dispose(); // Dispose the old one if updating
                return resizedBitmap;
            });


            return resizedBitmap;
        }
        catch (ArgumentNullException argNullEx) // Catch specific exception from BuildResourceName
        {
            Debug.WriteLine($"Error preparing resource name for '{relativePath}': {argNullEx.Message}");
            return null;
        }
        catch (InvalidOperationException invOpEx) // Catch specific exception from BuildResourceName
        {
            Debug.WriteLine($"Error accessing assembly metadata for '{relativePath}': {invOpEx.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Log general exceptions
            Debug.WriteLine($"Error loading embedded image '{relativePath}': {ex.GetType().Name} - {ex.Message}");
            // Consider logging stack trace ex.ToString() for detailed debugging
            return null;
        }
    }

    /// <summary>
    /// Loads an embedded SVG resource, renders it to a specific size (16x16), and caches the resulting Bitmap.
    /// Requires the 'Svg' (Svg.NET) NuGet package.
    /// </summary>
    /// <param name="relativePath">The relative path to the SVG resource within the assembly (e.g., "Resources/Icons/myicon.svg").</param>
    /// <returns>A 16x16 Bitmap representing the rendered SVG, or null if an error occurs.</returns>
    public static Bitmap? LoadEmbeddedSvgResized(string relativePath, int width = 16, int height = 16)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            Debug.WriteLine("Error (SVG): Relative path cannot be null or empty.");
            return null;
        }

        // Consider adding size to the cache key if you plan to support multiple sizes later
        string cacheKey = relativePath.ToLowerInvariant(); // Simple key for now, assumes only 16x16 needed

        // 1. Check cache
        if (_imageCache.TryGetValue(cacheKey, out var cachedBitmap))
        {
            if (cachedBitmap != null)
            {
                return cachedBitmap; // Return cached version
            }
            else
            {
                // Remove the invalid null entry if it exists
                _imageCache.TryRemove(cacheKey, out _);
            }
        }

        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = BuildResourceName(assembly, relativePath);

            if (resourceName == null)
            {
                // Error likely logged in BuildResourceName
                return null;
            }

            // 2. Get resource stream
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.WriteLine($"Error (SVG): Embedded resource not found or stream is null. Attempted Name: {resourceName}");
                return null;
            }

            // 3. Load SVG Document using Svg.NET
            // SvgDocument doesn't seem to be IDisposable based on common usage
            var svgDocument = SvgDocument.Open<SvgDocument>(stream);

            if (svgDocument == null)
            {
                Debug.WriteLine($"Error (SVG): Failed to load or parse SVG document from resource '{resourceName}'. Stream might be empty or invalid SVG.");
                return null;
            }

            // 4. Define target size and render the SVG to a Bitmap
            var desiredSize = new Size(width, height);

            // Svg.NET uses the Draw method which creates and returns the Bitmap
            // It's generally better to let the library handle bitmap creation.
            // Ensure the SVG's own size/viewbox doesn't prevent rendering at the desired size.
            // You might need to set svgDocument.Width/Height before drawing if default scaling isn't right.
            // svgDocument.Width = new SvgUnit(SvgUnitType.Pixel, desiredSize.Width);
            // svgDocument.Height = new SvgUnit(SvgUnitType.Pixel, desiredSize.Height);

            var resizedBitmap = svgDocument.Draw(desiredSize.Width, desiredSize.Height);

            if (resizedBitmap == null)
            {
                Debug.WriteLine($"Error (SVG): SvgDocument.Draw returned null for resource '{resourceName}' at size {desiredSize.Width}x{desiredSize.Height}.");
                return null;
            }

            // 5. Add to cache
            // Use AddOrUpdate for robustness
            _imageCache.AddOrUpdate(cacheKey, resizedBitmap, (key, existing) =>
            {
                existing?.Dispose(); // Dispose the old one if updating
                return resizedBitmap;
            });


            return resizedBitmap;
        }
        catch (ArgumentNullException argNullEx) // From BuildResourceName
        {
            Debug.WriteLine($"Error (SVG) preparing resource name for '{relativePath}': {argNullEx.Message}");
            return null;
        }
        catch (InvalidOperationException invOpEx) // From BuildResourceName or assembly access
        {
            Debug.WriteLine($"Error (SVG) accessing assembly metadata or resource for '{relativePath}': {invOpEx.Message}");
            return null;
        }
        catch (Exception ex) // Catch potential exceptions from SvgDocument.Open or Draw
        {
            Debug.WriteLine($"Error (SVG) loading or rendering embedded SVG '{relativePath}': {ex.GetType().Name} - {ex.Message}");
            // Consider logging ex.ToString() for detailed debugging, especially for SVG parsing errors
            return null;
        }
    }

    private static string BuildResourceName(Assembly assembly, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentNullException(nameof(relativePath), "Relative path cannot be null or whitespace.");
        }

        // 3. Get AssemblyName and check for null
        var assemblyName = assembly.GetName();
        if (assemblyName == null)
        {
            // Extremely unlikely, indicates severe assembly loading/metadata issue
            throw new InvalidOperationException($"Could not get AssemblyName for assembly: {assembly.FullName}. Cannot determine resource namespace.");
        }

        // Get root namespace and check for null/empty
        var rootNamespace = assembly.GetName()?.Name;
        if (string.IsNullOrEmpty(rootNamespace))
        {
            // Also very unlikely for a valid assembly
            throw new InvalidOperationException($"Assembly name (used as root namespace) is null or empty for assembly: {assembly.FullName}. Cannot determine resource namespace.");
        }


        // Combine namespace with the relative path, replacing directory separators with dots
        var FOLDER_SEPARATOR = ".";
        var cleanedRelativePath = relativePath
            .Replace("/", FOLDER_SEPARATOR)
            .Replace("\\", FOLDER_SEPARATOR)
            .TrimStart(FOLDER_SEPARATOR.ToCharArray()); // Ensure no leading dot if path starts with /

        // Construct the expected full resource name
        var defaultResourceName = $"{rootNamespace}.{cleanedRelativePath}";

        var availableResources = assembly.GetManifestResourceNames();
        if (availableResources == null || availableResources.Length == 0)
        {
            Debug.WriteLine($"Warning: Assembly '{rootNamespace}' contains no embedded resources.");
            // Proceed to check default name anyway, maybe the API call failed temporarily
        }
        else // Only search if resources exist
        {
            // Check if the default constructed name exists directly
            if (availableResources.Contains(defaultResourceName, StringComparer.OrdinalIgnoreCase))
            {
                return defaultResourceName;
            }

            // 4. Fallback search using EndsWith (FirstOrDefault null check is handled later)
            var endsWithPattern = FOLDER_SEPARATOR + cleanedRelativePath;
            var bestMatch = availableResources.FirstOrDefault(name =>
                name.EndsWith(endsWithPattern, StringComparison.OrdinalIgnoreCase));

            // Check the result of FirstOrDefault
            if (bestMatch != null)
            {
                return bestMatch; // Found a match via EndsWith
            }
        }


        // Last resort: Return the initially constructed name.
        // GetManifestResourceStream will return null later if this is incorrect.
        Debug.WriteLine($"Warning: Could not find an exact match or EndsWith match for resource '{relativePath}'. " +
                                $"Attempting default name: '{defaultResourceName}'. " +
                                $"Check build action ('Embedded Resource') and path/namespace spelling.");
        return defaultResourceName;
    }
    public static void ClearCache()
    {
        // Make disposal slightly safer
        foreach (var key in _imageCache.Keys)
        {
            if (_imageCache.TryRemove(key, out var bitmapToDispose))
            {
                bitmapToDispose?.Dispose();
            }
        }

        // Clear just in case TryRemove failed for some concurrent reason (unlikely)
        _imageCache.Clear();
    }
}
