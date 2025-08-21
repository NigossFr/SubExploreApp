using System;
using System.IO;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Compression algorithms supported by the service
    /// </summary>
    public enum CompressionAlgorithm
    {
        /// <summary>No compression</summary>
        None,
        
        /// <summary>GZip compression - best balance of speed and compression</summary>
        GZip,
        
        /// <summary>Deflate compression - slightly faster than GZip</summary>
        Deflate,
        
        /// <summary>Brotli compression - best compression ratio</summary>
        Brotli
    }

    /// <summary>
    /// Compression level settings
    /// </summary>
    public enum CompressionQuality
    {
        /// <summary>Fastest compression with lower ratio</summary>
        Fastest,
        
        /// <summary>Balanced speed and compression</summary>
        Optimal,
        
        /// <summary>Smallest size with slower compression</summary>
        SmallestSize
    }

    /// <summary>
    /// Compression operation result with performance metrics
    /// </summary>
    public class CompressionResult
    {
        public bool IsSuccess { get; set; }
        public byte[] CompressedData { get; set; } = Array.Empty<byte>();
        public int OriginalSize { get; set; }
        public int CompressedSize { get; set; }
        public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 0;
        public double SpaceSaved => OriginalSize > 0 ? 1 - CompressionRatio : 0;
        public TimeSpan ProcessingTime { get; set; }
        public CompressionAlgorithm Algorithm { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Decompression operation result with validation
    /// </summary>
    public class DecompressionResult
    {
        public bool IsSuccess { get; set; }
        public byte[] DecompressedData { get; set; } = Array.Empty<byte>();
        public int DecompressedSize { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public CompressionAlgorithm Algorithm { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// High-performance compression service for reducing data transfer sizes
    /// Supports multiple algorithms, automatic algorithm selection, and performance optimization
    /// </summary>
    public interface ICompressionService
    {
        /// <summary>
        /// Compresses data using specified algorithm and level
        /// </summary>
        /// <param name="data">Data to compress</param>
        /// <param name="algorithm">Compression algorithm to use</param>
        /// <param name="level">Compression level</param>
        /// <returns>Compression result with metrics</returns>
        Task<CompressionResult> CompressAsync(
            byte[] data,
            CompressionAlgorithm algorithm = CompressionAlgorithm.GZip,
            CompressionQuality level = CompressionQuality.Optimal);

        /// <summary>
        /// Compresses text data with automatic UTF-8 encoding
        /// </summary>
        /// <param name="text">Text data to compress</param>
        /// <param name="algorithm">Compression algorithm to use</param>
        /// <param name="level">Compression level</param>
        /// <returns>Compression result with metrics</returns>
        Task<CompressionResult> CompressTextAsync(
            string text,
            CompressionAlgorithm algorithm = CompressionAlgorithm.GZip,
            CompressionQuality level = CompressionQuality.Optimal);

        /// <summary>
        /// Decompresses data using specified algorithm
        /// </summary>
        /// <param name="compressedData">Compressed data</param>
        /// <param name="algorithm">Compression algorithm used</param>
        /// <returns>Decompression result with validation</returns>
        Task<DecompressionResult> DecompressAsync(
            byte[] compressedData,
            CompressionAlgorithm algorithm);

        /// <summary>
        /// Decompresses data and converts to text using UTF-8 encoding
        /// </summary>
        /// <param name="compressedData">Compressed data</param>
        /// <param name="algorithm">Compression algorithm used</param>
        /// <returns>Decompressed text string</returns>
        Task<string> DecompressTextAsync(
            byte[] compressedData,
            CompressionAlgorithm algorithm);

        /// <summary>
        /// Automatically selects best compression algorithm based on data characteristics
        /// </summary>
        /// <param name="data">Data to analyze</param>
        /// <param name="targetCompressionRatio">Desired compression ratio (0-1)</param>
        /// <returns>Recommended algorithm and level</returns>
        Task<(CompressionAlgorithm algorithm, CompressionQuality level)> SelectOptimalAlgorithmAsync(
            byte[] data,
            double targetCompressionRatio = 0.5);

        /// <summary>
        /// Compresses HTTP response content with appropriate headers
        /// </summary>
        /// <param name="content">Response content to compress</param>
        /// <param name="acceptedEncodings">Client-accepted encodings</param>
        /// <returns>Compression result with encoding information</returns>
        Task<CompressionResult> CompressHttpResponseAsync(
            byte[] content,
            string[] acceptedEncodings);

        /// <summary>
        /// Compresses stream data with memory-efficient processing
        /// </summary>
        /// <param name="inputStream">Input stream to compress</param>
        /// <param name="outputStream">Output stream for compressed data</param>
        /// <param name="algorithm">Compression algorithm to use</param>
        /// <param name="level">Compression level</param>
        /// <returns>Compression statistics</returns>
        Task<CompressionResult> CompressStreamAsync(
            Stream inputStream,
            Stream outputStream,
            CompressionAlgorithm algorithm = CompressionAlgorithm.GZip,
            CompressionQuality level = CompressionQuality.Optimal);

        /// <summary>
        /// Gets compression service performance statistics
        /// </summary>
        /// <returns>Performance metrics and usage statistics</returns>
        Task<CompressionStatistics> GetStatisticsAsync();

        /// <summary>
        /// Estimates compression benefit for given data
        /// </summary>
        /// <param name="data">Data to analyze</param>
        /// <returns>Estimated compression ratios for different algorithms</returns>
        Task<Dictionary<CompressionAlgorithm, double>> EstimateCompressionBenefitAsync(byte[] data);
    }

    /// <summary>
    /// Compression service performance statistics
    /// </summary>
    public class CompressionStatistics
    {
        public long TotalCompressions { get; set; }
        public long TotalDecompressions { get; set; }
        public long TotalBytesProcessed { get; set; }
        public long TotalBytesSaved { get; set; }
        public double AverageCompressionRatio { get; set; }
        public double AverageSpaceSaved => 1 - AverageCompressionRatio;
        public TimeSpan AverageCompressionTime { get; set; }
        public TimeSpan AverageDecompressionTime { get; set; }
        public Dictionary<CompressionAlgorithm, long> AlgorithmUsageStats { get; set; } = new();
        public Dictionary<CompressionQuality, long> LevelUsageStats { get; set; } = new();
        public DateTime LastResetTime { get; set; }
        public long ErrorCount { get; set; }

        /// <summary>
        /// Gets formatted statistics summary
        /// </summary>
        public string GetSummary()
        {
            return $"Compression: {TotalCompressions} operations, " +
                   $"{TotalBytesSaved / (1024 * 1024):F1}MB saved ({AverageSpaceSaved:P1}), " +
                   $"Avg: {AverageCompressionTime.TotalMilliseconds:F1}ms, " +
                   $"{ErrorCount} errors";
        }
    }
}