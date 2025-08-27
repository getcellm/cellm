using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using Svg;

namespace Cellm.AddIn.UserInterface.Ribbon;

public static class ImageLoader
{
    private static readonly ConcurrentDictionary<string, Bitmap> _imageCache =
        new ConcurrentDictionary<string, Bitmap>();

    /// <summary>
    /// Loads an embedded PNG image, resizes it using high-quality settings, and caches the result.
    /// </summary>
    /// <param name="relativePath">The relative path to the resource within the assembly (e.g., "Images/Icons/my_icon.png").</param>
    /// <param name="width">The desired width of the resized image.</param>
    /// <param name="height">The desired height of the resized image.</param>
    /// <returns>A resized Bitmap, or null if an error occurs.</returns>
    public static Bitmap? LoadEmbeddedPngResized(string relativePath, int width = 16, int height = 16)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            Debug.WriteLine("Error: Relative path cannot be null or empty.");
            return null;
        }
        // Ensure consistent positive dimensions for resizing
        if (width <= 0 || height <= 0)
        {
            Debug.WriteLine($"Error: Invalid dimensions requested ({width}x{height}). Using defaults (16x16).");
            width = 16;
            height = 16;
        }

        var cacheKey = $"{relativePath.ToLowerInvariant()}_{width}x{height}"; // Include size in cache key

        // 1. Check cache
        if (_imageCache.TryGetValue(cacheKey, out var cachedBitmap))
        {
            // Return a clone from cache to prevent caller disposing the cached instance?
            // For now, returning the cached instance directly as per original logic.
            // Be mindful that multiple callers might share the same Bitmap instance.
            if (cachedBitmap != null)
            {
                // Optional: Return a clone if you want callers to have independent instances
                // return new Bitmap(cachedBitmap);
                return cachedBitmap;
            }
            else
            {
                _imageCache.TryRemove(cacheKey, out _); // Clean up bad entry
            }
        }

        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = BuildResourceName(assembly, relativePath);

            if (resourceName == null)
            {
                // Error logged in BuildResourceName or exception thrown
                return null;
            }

            // 2. Get resource stream
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Debug.WriteLine($"Error: Embedded resource not found or stream is null. Attempted Name: {resourceName}");
                // Cache a specific marker or handle differently? For now, just return null.
                return null;
            }

            // Load the original bitmap from the stream
            using var originalBitmap = new Bitmap(stream);

            // Create a new bitmap with the desired dimensions and original pixel format.
            // Using original PixelFormat is crucial for transparency (alpha channel).
            var resizedBitmap = new Bitmap(width, height, originalBitmap.PixelFormat);

            // Set the resolution of the new bitmap (optional but good practice)
            resizedBitmap.SetResolution(originalBitmap.HorizontalResolution, originalBitmap.VerticalResolution);

            // Get a Graphics object from the new bitmap
            using (var graphics = Graphics.FromImage(resizedBitmap))
            {
                // Set the quality settings for the resizing operation
                graphics.CompositingMode = CompositingMode.SourceCopy; // Crucial for transparency
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; // Best quality interpolation
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Use ImageAttributes to prevent potential border artifacts (optional but good)
                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY); // Prevents edge artifacts

                // Define source (entire original image) and destination rectangles
                var sourceRect = new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height);
                var destRect = new Rectangle(0, 0, width, height);

                // Draw the original image onto the new bitmap canvas using high-quality settings
                graphics.DrawImage(originalBitmap, destRect, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, GraphicsUnit.Pixel, wrapMode);
            } // Graphics object is disposed here

            // --- END: High-Quality Resizing Logic ---


            // Add the newly created resized bitmap to the cache.
            // AddOrUpdate handles the case where another thread might have added it already.
            var addedOrUpdatedBitmap = _imageCache.AddOrUpdate(cacheKey, resizedBitmap, (key, existing) =>
            {
                // This factory runs if the key exists.
                // We created 'resizedBitmap' *before* the AddOrUpdate.
                // If we won the race, 'resizedBitmap' is added.
                // If another thread added one first, 'existing' will be that bitmap.
                // We should dispose the 'resizedBitmap' we created if it wasn't added.
                // However, AddOrUpdate returns the *actual* value in the dictionary.
                // If it's not our instance, we need to dispose ours.

                // If the existing one is different from the one we just created, dispose the one we created
                // as it won't be used or cached. The 'existing' one is kept in the cache.
                if (existing != resizedBitmap)
                {
                    resizedBitmap.Dispose(); // Dispose the bitmap we created but didn't cache
                }
                return existing; // Keep the existing one
            });

            // If AddOrUpdate decided to keep an *existing* bitmap added by another thread
            // just before ours, we need to return *that* one, not the one we created and potentially disposed.
            // If AddOrUpdate *added* our bitmap, addedOrUpdatedBitmap will be == resizedBitmap.
            if (addedOrUpdatedBitmap != resizedBitmap)
            {
                // This means another thread added an item between our TryGetValue and AddOrUpdate.
                // The 'updateValueFactory' above already disposed our 'resizedBitmap'.
                // We return the instance that is actually in the cache.
                return addedOrUpdatedBitmap;
            }


            // Return the bitmap that was successfully created and cached (could be ours or one from another thread)
            return resizedBitmap; // Return the bitmap instance that IS in the cache
        }
        catch (ArgumentNullException argNullEx)
        {
            Debug.WriteLine($"Error preparing resource name for '{relativePath}': {argNullEx.Message}");
            return null;
        }
        catch (InvalidOperationException invOpEx)
        {
            Debug.WriteLine($"Error accessing assembly metadata for '{relativePath}': {invOpEx.Message}");
            return null;
        }
        catch (Exception ex) // Catch broader exceptions during Bitmap/Graphics operations too
        {
            Debug.WriteLine($"Error loading/resizing embedded image '{relativePath}': {ex.GetType().Name} - {ex.Message}");
            // Consider logging stack trace ex.ToString() for detailed debugging
            _imageCache.TryRemove(cacheKey, out _); // Attempt to remove potentially corrupted cache entry on error
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
