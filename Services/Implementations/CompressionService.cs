using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// High-performance compression service with multiple algorithm support
    /// </summary>
    public class CompressionService : ICompressionService
    {
        private readonly ILogger<CompressionService> _logger;
        private readonly CompressionStatistics _statistics;

        // Algorithm preference order for HTTP responses
        private static readonly CompressionAlgorithm[] HttpAlgorithmPreference = 
        {
            CompressionAlgorithm.Brotli,
            CompressionAlgorithm.GZip,
            CompressionAlgorithm.Deflate
        };

        // HTTP encoding names mapping
        private static readonly Dictionary<CompressionAlgorithm, string> HttpEncodingNames = new()
        {
            { CompressionAlgorithm.GZip, "gzip" },
            { CompressionAlgorithm.Deflate, "deflate" },
            { CompressionAlgorithm.Brotli, "br" }
        };

        public CompressionService(ILogger<CompressionService> logger)
        {
            _logger = logger;
            _statistics = new CompressionStatistics { LastResetTime = DateTime.UtcNow };
        }

        /// <summary>
        /// Compresses data with specified algorithm and performance tracking
        /// </summary>
        public async Task<CompressionResult> CompressAsync(
            byte[] data,
            CompressionAlgorithm algorithm = CompressionAlgorithm.GZip,
            CompressionQuality level = CompressionQuality.Optimal)
        {
            if (data == null || data.Length == 0)
            {
                return new CompressionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Input data is null or empty",
                    Algorithm = algorithm
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var result = new CompressionResult
            {
                OriginalSize = data.Length,
                Algorithm = algorithm
            };

            try
            {
                _logger.LogDebug("🗜️ Compressing {Size}KB with {Algorithm} at {Level} level",
                    data.Length / 1024.0, algorithm, level);

                using var inputStream = new MemoryStream(data);
                using var outputStream = new MemoryStream();

                await CompressStreamInternalAsync(inputStream, outputStream, algorithm, level);

                result.CompressedData = outputStream.ToArray();
                result.CompressedSize = result.CompressedData.Length;
                result.IsSuccess = true;

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;

                // Update statistics
                UpdateCompressionStatistics(result);

                _logger.LogDebug("✅ Compression completed: {OriginalSize}KB → {CompressedSize}KB ({Ratio:P1}) in {Ms}ms",
                    result.OriginalSize / 1024.0, result.CompressedSize / 1024.0, result.SpaceSaved, result.ProcessingTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _statistics.ErrorCount++;

                _logger.LogError(ex, "❌ Compression failed for {Algorithm}", algorithm);
                return result;
            }
        }

        /// <summary>
        /// Compresses text with UTF-8 encoding
        /// </summary>
        public async Task<CompressionResult> CompressTextAsync(
            string text,
            CompressionAlgorithm algorithm = CompressionAlgorithm.GZip,
            CompressionQuality level = CompressionQuality.Optimal)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new CompressionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Input text is null or empty",
                    Algorithm = algorithm
                };
            }

            var textBytes = Encoding.UTF8.GetBytes(text);
            return await CompressAsync(textBytes, algorithm, level);
        }

        /// <summary>
        /// Decompresses data with validation and error handling
        /// </summary>
        public async Task<DecompressionResult> DecompressAsync(
            byte[] compressedData,
            CompressionAlgorithm algorithm)
        {
            if (compressedData == null || compressedData.Length == 0)
            {
                return new DecompressionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Compressed data is null or empty",
                    Algorithm = algorithm
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var result = new DecompressionResult { Algorithm = algorithm };

            try
            {
                _logger.LogDebug("📂 Decompressing {Size}KB with {Algorithm}",
                    compressedData.Length / 1024.0, algorithm);

                using var inputStream = new MemoryStream(compressedData);
                using var outputStream = new MemoryStream();

                await DecompressStreamInternalAsync(inputStream, outputStream, algorithm);

                result.DecompressedData = outputStream.ToArray();
                result.DecompressedSize = result.DecompressedData.Length;
                result.IsSuccess = true;

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;

                _statistics.TotalDecompressions++;
                var currentAvgTicks = _statistics.AverageDecompressionTime.Ticks;
                _statistics.AverageDecompressionTime = TimeSpan.FromTicks((currentAvgTicks + result.ProcessingTime.Ticks) / 2);

                _logger.LogDebug("✅ Decompression completed: {Size}KB in {Ms}ms",
                    result.DecompressedSize / 1024.0, result.ProcessingTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _statistics.ErrorCount++;

                _logger.LogError(ex, "❌ Decompression failed for {Algorithm}", algorithm);
                return result;
            }
        }

        /// <summary>
        /// Decompresses data and converts to UTF-8 text
        /// </summary>
        public async Task<string> DecompressTextAsync(
            byte[] compressedData,
            CompressionAlgorithm algorithm)
        {
            var result = await DecompressAsync(compressedData, algorithm);
            
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Decompression failed: {result.ErrorMessage}");
            }

            return Encoding.UTF8.GetString(result.DecompressedData);
        }

        /// <summary>
        /// Automatically selects optimal compression algorithm based on data analysis
        /// </summary>
        public async Task<(CompressionAlgorithm algorithm, CompressionQuality level)> SelectOptimalAlgorithmAsync(
            byte[] data,
            double targetCompressionRatio = 0.5)
        {
            if (data == null || data.Length == 0)
                return (CompressionAlgorithm.None, CompressionQuality.Fastest);

            _logger.LogDebug("🔍 Analyzing data for optimal compression algorithm");

            // Quick analysis of data characteristics
            var entropy = CalculateEntropy(data);
            var repetitionScore = CalculateRepetitionScore(data.Take(Math.Min(1024, data.Length)).ToArray());

            _logger.LogDebug("📊 Data analysis: Entropy={Entropy:F2}, Repetition={Repetition:F2}", entropy, repetitionScore);

            // Algorithm selection logic based on data characteristics
            CompressionAlgorithm selectedAlgorithm;
            CompressionQuality selectedLevel;

            if (data.Length < 1024)
            {
                // Small data: use fastest algorithm
                selectedAlgorithm = CompressionAlgorithm.Deflate;
                selectedLevel = CompressionQuality.Fastest;
            }
            else if (entropy > 7.0)
            {
                // High entropy (likely already compressed or random): minimal compression
                selectedAlgorithm = CompressionAlgorithm.Deflate;
                selectedLevel = CompressionQuality.Fastest;
            }
            else if (repetitionScore > 0.3 && targetCompressionRatio < 0.4)
            {
                // High repetition and aggressive compression target: use Brotli
                selectedAlgorithm = CompressionAlgorithm.Brotli;
                selectedLevel = CompressionQuality.SmallestSize;
            }
            else
            {
                // General case: balanced GZip compression
                selectedAlgorithm = CompressionAlgorithm.GZip;
                selectedLevel = CompressionQuality.Optimal;
            }

            _logger.LogDebug("✅ Selected algorithm: {Algorithm} with {Level} level", selectedAlgorithm, selectedLevel);
            
            await Task.CompletedTask;
            return (selectedAlgorithm, selectedLevel);
        }

        /// <summary>
        /// Compresses HTTP response with client-compatible encoding
        /// </summary>
        public async Task<CompressionResult> CompressHttpResponseAsync(
            byte[] content,
            string[] acceptedEncodings)
        {
            if (acceptedEncodings == null || !acceptedEncodings.Any())
            {
                return await CompressAsync(content, CompressionAlgorithm.None);
            }

            // Find the best supported algorithm
            var selectedAlgorithm = CompressionAlgorithm.None;
            
            foreach (var preferredAlgorithm in HttpAlgorithmPreference)
            {
                var encodingName = HttpEncodingNames.GetValueOrDefault(preferredAlgorithm);
                if (encodingName != null && acceptedEncodings.Contains(encodingName, StringComparer.OrdinalIgnoreCase))
                {
                    selectedAlgorithm = preferredAlgorithm;
                    break;
                }
            }

            _logger.LogDebug("🌐 HTTP compression: Selected {Algorithm} from accepted encodings: {Encodings}",
                selectedAlgorithm, string.Join(", ", acceptedEncodings));

            return await CompressAsync(content, selectedAlgorithm, CompressionQuality.Optimal);
        }

        /// <summary>
        /// Compresses stream data with memory efficiency
        /// </summary>
        public async Task<CompressionResult> CompressStreamAsync(
            Stream inputStream,
            Stream outputStream,
            CompressionAlgorithm algorithm = CompressionAlgorithm.GZip,
            CompressionQuality level = CompressionQuality.Optimal)
        {
            if (inputStream == null || outputStream == null)
                throw new ArgumentNullException("Input and output streams cannot be null");

            var stopwatch = Stopwatch.StartNew();
            var originalPosition = inputStream.Position;
            var originalSize = (int)(inputStream.Length - inputStream.Position);

            try
            {
                await CompressStreamInternalAsync(inputStream, outputStream, algorithm, level);

                stopwatch.Stop();
                var compressedSize = (int)outputStream.Length;

                var result = new CompressionResult
                {
                    IsSuccess = true,
                    OriginalSize = originalSize,
                    CompressedSize = compressedSize,
                    ProcessingTime = stopwatch.Elapsed,
                    Algorithm = algorithm
                };

                UpdateCompressionStatistics(result);
                
                _logger.LogDebug("✅ Stream compression: {OriginalSize}KB → {CompressedSize}KB ({Ratio:P1})",
                    originalSize / 1024.0, compressedSize / 1024.0, result.SpaceSaved);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _statistics.ErrorCount++;
                _logger.LogError(ex, "❌ Stream compression failed");
                
                return new CompressionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Algorithm = algorithm,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// Gets comprehensive compression service statistics
        /// </summary>
        public async Task<CompressionStatistics> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            return _statistics;
        }

        /// <summary>
        /// Estimates compression benefit for different algorithms
        /// </summary>
        public async Task<Dictionary<CompressionAlgorithm, double>> EstimateCompressionBenefitAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
                return new Dictionary<CompressionAlgorithm, double>();

            _logger.LogDebug("📈 Estimating compression benefits for {Size}KB", data.Length / 1024.0);

            var estimates = new Dictionary<CompressionAlgorithm, double>();
            var sampleData = data.Length > 4096 ? data.Take(4096).ToArray() : data;

            // Quick estimation based on entropy and repetition
            var entropy = CalculateEntropy(sampleData);
            var repetition = CalculateRepetitionScore(sampleData);

            // Estimate compression ratios (these are heuristic approximations)
            estimates[CompressionAlgorithm.None] = 1.0;
            estimates[CompressionAlgorithm.Deflate] = Math.Max(0.3, 1.0 - (0.4 * (1 - entropy / 8.0) + 0.3 * repetition));
            estimates[CompressionAlgorithm.GZip] = Math.Max(0.25, estimates[CompressionAlgorithm.Deflate] * 0.95);
            estimates[CompressionAlgorithm.Brotli] = Math.Max(0.2, estimates[CompressionAlgorithm.GZip] * 0.9);

            _logger.LogDebug("📊 Estimated ratios - Deflate: {Deflate:P1}, GZip: {GZip:P1}, Brotli: {Brotli:P1}",
                estimates[CompressionAlgorithm.Deflate], estimates[CompressionAlgorithm.GZip], estimates[CompressionAlgorithm.Brotli]);

            await Task.CompletedTask;
            return estimates;
        }

        #region Private Helper Methods

        /// <summary>
        /// Internal stream compression with algorithm-specific handling
        /// </summary>
        private async Task CompressStreamInternalAsync(
            Stream inputStream,
            Stream outputStream,
            CompressionAlgorithm algorithm,
            CompressionQuality level)
        {
            var compressionLevel = MapCompressionLevel(level);

            Stream compressionStream = algorithm switch
            {
                CompressionAlgorithm.GZip => new GZipStream(outputStream, compressionLevel, leaveOpen: true),
                CompressionAlgorithm.Deflate => new DeflateStream(outputStream, compressionLevel, leaveOpen: true),
                CompressionAlgorithm.Brotli => new BrotliStream(outputStream, compressionLevel, leaveOpen: true),
                CompressionAlgorithm.None => outputStream,
                _ => throw new NotSupportedException($"Algorithm {algorithm} not supported")
            };

            try
            {
                if (algorithm == CompressionAlgorithm.None)
                {
                    await inputStream.CopyToAsync(outputStream);
                }
                else
                {
                    using (compressionStream)
                    {
                        await inputStream.CopyToAsync(compressionStream);
                        await compressionStream.FlushAsync();
                    }
                }
            }
            finally
            {
                if (compressionStream != outputStream)
                {
                    compressionStream?.Dispose();
                }
            }
        }

        /// <summary>
        /// Internal stream decompression with algorithm-specific handling
        /// </summary>
        private async Task DecompressStreamInternalAsync(
            Stream inputStream,
            Stream outputStream,
            CompressionAlgorithm algorithm)
        {
            Stream decompressionStream = algorithm switch
            {
                CompressionAlgorithm.GZip => new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                CompressionAlgorithm.Deflate => new DeflateStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                CompressionAlgorithm.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress, leaveOpen: true),
                CompressionAlgorithm.None => inputStream,
                _ => throw new NotSupportedException($"Algorithm {algorithm} not supported")
            };

            try
            {
                if (algorithm == CompressionAlgorithm.None)
                {
                    await inputStream.CopyToAsync(outputStream);
                }
                else
                {
                    using (decompressionStream)
                    {
                        await decompressionStream.CopyToAsync(outputStream);
                    }
                }
            }
            finally
            {
                if (decompressionStream != inputStream)
                {
                    decompressionStream?.Dispose();
                }
            }
        }

        /// <summary>
        /// Maps service compression level to system compression level
        /// </summary>
        private static System.IO.Compression.CompressionLevel MapCompressionLevel(CompressionQuality level)
        {
            return level switch
            {
                CompressionQuality.Fastest => System.IO.Compression.CompressionLevel.Fastest,
                CompressionQuality.Optimal => System.IO.Compression.CompressionLevel.Optimal,
                CompressionQuality.SmallestSize => System.IO.Compression.CompressionLevel.SmallestSize,
                _ => System.IO.Compression.CompressionLevel.Optimal
            };
        }

        /// <summary>
        /// Calculates Shannon entropy for data analysis
        /// </summary>
        private static double CalculateEntropy(byte[] data)
        {
            if (data.Length == 0) return 0;

            var frequency = new int[256];
            foreach (var b in data)
            {
                frequency[b]++;
            }

            double entropy = 0;
            var length = data.Length;

            for (int i = 0; i < 256; i++)
            {
                if (frequency[i] > 0)
                {
                    var probability = (double)frequency[i] / length;
                    entropy -= probability * Math.Log2(probability);
                }
            }

            return entropy;
        }

        /// <summary>
        /// Calculates repetition score for compression potential analysis
        /// </summary>
        private static double CalculateRepetitionScore(byte[] data)
        {
            if (data.Length < 4) return 0;

            var patterns = new Dictionary<uint, int>();
            var totalPatterns = 0;

            // Look for 4-byte patterns
            for (int i = 0; i <= data.Length - 4; i++)
            {
                var pattern = BitConverter.ToUInt32(data, i);
                patterns[pattern] = patterns.GetValueOrDefault(pattern, 0) + 1;
                totalPatterns++;
            }

            var repeatedPatterns = patterns.Values.Where(count => count > 1).Sum(count => count - 1);
            return totalPatterns > 0 ? (double)repeatedPatterns / totalPatterns : 0;
        }

        /// <summary>
        /// Updates compression statistics with operation results
        /// </summary>
        private void UpdateCompressionStatistics(CompressionResult result)
        {
            _statistics.TotalCompressions++;
            _statistics.TotalBytesProcessed += result.OriginalSize;
            _statistics.TotalBytesSaved += Math.Max(0, result.OriginalSize - result.CompressedSize);

            // Update algorithm usage stats
            if (!_statistics.AlgorithmUsageStats.ContainsKey(result.Algorithm))
                _statistics.AlgorithmUsageStats[result.Algorithm] = 0;
            _statistics.AlgorithmUsageStats[result.Algorithm]++;

            // Update average compression ratio and time
            var currentAvgRatio = _statistics.AverageCompressionRatio;
            _statistics.AverageCompressionRatio = (currentAvgRatio + result.CompressionRatio) / 2;

            var currentAvgTime = _statistics.AverageCompressionTime;
            _statistics.AverageCompressionTime = TimeSpan.FromTicks((currentAvgTime.Ticks + result.ProcessingTime.Ticks) / 2);
        }

        #endregion
    }
}