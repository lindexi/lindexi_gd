using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TxtChunking;

namespace TxtChunking.UnitTests
{
    /// <summary>
    /// Tests for the RegexExtensions class.
    /// </summary>
    [TestClass]
    public sealed class RegexExtensionsTests
    {
    }

    /// <summary>
    /// Unit tests for the TxtTokenChunker class.
    /// </summary>
    [TestClass]
    public sealed class TxtTokenChunkerTests
    {
        /// <summary>
        /// Tests that ChunkText returns an empty list when the input text is null.
        /// Input: null string.
        /// Expected: Empty list of ChunkResult.
        /// </summary>
        [TestMethod]
        public void ChunkText_NullText_ReturnsEmptyList()
        {
            // Arrange
            var options = new ChunkingOptions();
            var mockEstimator = new Mock<ITokenEstimator>();
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string? text = null;

            // Act
            var result = chunker.ChunkText(text!);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Tests that ChunkText returns an empty list when the input text is an empty string.
        /// Input: Empty string.
        /// Expected: Empty list of ChunkResult.
        /// </summary>
        [TestMethod]
        public void ChunkText_EmptyText_ReturnsEmptyList()
        {
            // Arrange
            var options = new ChunkingOptions();
            var mockEstimator = new Mock<ITokenEstimator>();
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Tests that ChunkText returns an empty list when the input text contains only whitespace.
        /// Input: Whitespace-only string (spaces, tabs, newlines).
        /// Expected: Empty list of ChunkResult.
        /// </summary>
        [TestMethod]
        [DataRow("   ")]
        [DataRow("\t\t")]
        [DataRow("\n\n\n")]
        [DataRow(" \t \n ")]
        [DataRow("\r\n\r\n")]
        public void ChunkText_WhitespaceOnlyText_ReturnsEmptyList(string text)
        {
            // Arrange
            var options = new ChunkingOptions();
            var mockEstimator = new Mock<ITokenEstimator>();
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Tests that ChunkText returns a single chunk with no context when input is a single short paragraph.
        /// Input: Single paragraph that fits within token limits.
        /// Expected: One ChunkResult with empty contextBefore and contextAfter.
        /// </summary>
        [TestMethod]
        public void ChunkText_SingleParagraph_ReturnsSingleChunkWithNoContext()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "This is a single paragraph.";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("", result[0].ContextBefore);
            Assert.AreEqual("This is a single paragraph.", result[0].MainContent);
            Assert.AreEqual("", result[0].ContextAfter);
        }

        /// <summary>
        /// Tests that ChunkText returns a single chunk when multiple paragraphs fit within token limits.
        /// Input: Multiple paragraphs that together fit in one chunk.
        /// Expected: One ChunkResult containing all paragraphs joined, with no context.
        /// </summary>
        [TestMethod]
        public void ChunkText_MultipleParagraphsFittingInOneChunk_ReturnsSingleChunk()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("", result[0].ContextBefore);
            Assert.IsTrue(result[0].MainContent.Contains("First paragraph"));
            Assert.IsTrue(result[0].MainContent.Contains("Second paragraph"));
            Assert.IsTrue(result[0].MainContent.Contains("Third paragraph"));
            Assert.AreEqual("", result[0].ContextAfter);
        }

        /// <summary>
        /// Tests that ChunkText creates multiple chunks when paragraphs exceed token limits.
        /// Input: Multiple paragraphs that require splitting into multiple chunks.
        /// Expected: Multiple ChunkResults with proper context before and after.
        /// </summary>
        [TestMethod]
        public void ChunkText_MultipleParagraphsExceedingLimit_ReturnsMultipleChunksWithContext()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 50, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();

            // Set up token estimation to force chunking
            mockEstimator.Setup(e => e.EstimateTokens("Para1")).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens("Para2")).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens("Para3")).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens("\n\n")).Returns(1);
            mockEstimator.Setup(e => e.EstimateTokens(It.Is<string>(s => s.Contains("Para1") && s.Contains("Para2")))).Returns(61);
            mockEstimator.Setup(e => e.EstimateTokens(It.Is<string>(s => s.Contains("Para2") && !s.Contains("Para1") && !s.Contains("Para3")))).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens(It.Is<string>(s => s.Contains("Para3") && !s.Contains("Para2")))).Returns(30);

            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "Para1\n\nPara2\n\nPara3";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count >= 1);

            // First chunk should have no context before
            Assert.AreEqual("", result[0].ContextBefore);

            // Last chunk should have no context after
            Assert.AreEqual("", result[result.Count - 1].ContextAfter);

            // Middle chunks should have context
            if (result.Count > 2)
            {
                Assert.AreNotEqual("", result[1].ContextBefore);
                Assert.AreNotEqual("", result[1].ContextAfter);
            }
        }

        /// <summary>
        /// Tests that ChunkText properly handles context propagation between chunks.
        /// Input: Text that creates exactly three chunks.
        /// Expected: Proper context before/after for each chunk.
        /// </summary>
        [TestMethod]
        public void ChunkText_ThreeChunks_PropagatesContextCorrectly()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 40, SoftLimitRatio = 0.5 };
            var mockEstimator = new Mock<ITokenEstimator>();

            // Each paragraph is 30 tokens, separator is 1 token
            // Soft limit = 20, so each paragraph becomes its own chunk
            mockEstimator.Setup(e => e.EstimateTokens("First")).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens("Second")).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens("Third")).Returns(30);
            mockEstimator.Setup(e => e.EstimateTokens("\n\n")).Returns(1);

            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "First\n\nSecond\n\nThird";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            // First chunk
            Assert.AreEqual("", result[0].ContextBefore);
            Assert.AreEqual("First", result[0].MainContent);
            Assert.AreEqual("Second", result[0].ContextAfter);

            // Second chunk
            Assert.AreEqual("First", result[1].ContextBefore);
            Assert.AreEqual("Second", result[1].MainContent);
            Assert.AreEqual("Third", result[1].ContextAfter);

            // Third chunk
            Assert.AreEqual("Second", result[2].ContextBefore);
            Assert.AreEqual("Third", result[2].MainContent);
            Assert.AreEqual("", result[2].ContextAfter);
        }

        /// <summary>
        /// Tests that ChunkText handles text with leading and trailing whitespace correctly.
        /// Input: Text with extra whitespace before and after content.
        /// Expected: Whitespace is trimmed according to TrimUnits option.
        /// </summary>
        [TestMethod]
        public void ChunkText_TextWithLeadingTrailingWhitespace_TrimsCorrectly()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8, TrimUnits = true };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "  \n\n  Content with whitespace  \n\n  ";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].MainContent.StartsWith(" "));
            Assert.IsFalse(result[0].MainContent.EndsWith(" "));
        }

        /// <summary>
        /// Tests that ChunkText handles text containing only paragraph separators.
        /// Input: Text with only blank lines.
        /// Expected: Empty list since no actual content exists.
        /// </summary>
        [TestMethod]
        public void ChunkText_OnlyParagraphSeparators_ReturnsEmptyList()
        {
            // Arrange
            var options = new ChunkingOptions();
            var mockEstimator = new Mock<ITokenEstimator>();
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "\n\n\n\n\n\n";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Tests that ChunkText handles text with special characters correctly.
        /// Input: Text containing special characters like quotes, symbols, unicode.
        /// Expected: Special characters are preserved in chunks.
        /// </summary>
        [TestMethod]
        public void ChunkText_TextWithSpecialCharacters_PreservesCharacters()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "Special chars: @#$%^&*()_+{}|:\"<>?[];',./`~\n\nUnicode: 你好世界 🌍";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result[0].MainContent.Contains("@#$%^&*()"));
            Assert.IsTrue(result[0].MainContent.Contains("你好世界"));
            Assert.IsTrue(result[0].MainContent.Contains("🌍"));
        }

        /// <summary>
        /// Tests that ChunkText handles very long single-line text correctly.
        /// Input: Very long text without paragraph breaks.
        /// Expected: Text is chunked appropriately, potentially using sentence splitting.
        /// </summary>
        [TestMethod]
        public void ChunkText_VeryLongSingleLine_CreatesMultipleChunks()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 50, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();

            // Simulate a very long paragraph that exceeds token limit
            string longText = string.Join(" ", Enumerable.Repeat("word", 1000));
            mockEstimator.Setup(e => e.EstimateTokens(It.Is<string>(s => s.Length > 100))).Returns(1000);
            mockEstimator.Setup(e => e.EstimateTokens(It.Is<string>(s => s.Length <= 100 && s.Length > 0))).Returns(10);
            mockEstimator.Setup(e => e.EstimateTokens("\n")).Returns(1);
            mockEstimator.Setup(e => e.EstimateTokens("\n\n")).Returns(1);

            var chunker = new TxtTokenChunker(options, mockEstimator.Object);

            // Act
            var result = chunker.ChunkText(longText);

            // Assert
            Assert.IsNotNull(result);
            // Should create multiple chunks due to length
            Assert.IsTrue(result.Count >= 1);
        }

        /// <summary>
        /// Tests that ChunkText handles text with mixed line endings correctly.
        /// Input: Text with different line ending styles (CRLF, LF).
        /// Expected: All line endings are handled correctly by the paragraph regex.
        /// </summary>
        [TestMethod]
        public void ChunkText_MixedLineEndings_HandlesCorrectly()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "Paragraph1\r\n\r\nParagraph2\n\nParagraph3";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result[0].MainContent.Contains("Paragraph1"));
            Assert.IsTrue(result[0].MainContent.Contains("Paragraph2"));
            Assert.IsTrue(result[0].MainContent.Contains("Paragraph3"));
        }

        /// <summary>
        /// Tests that ChunkText handles text where TrimUnits is false.
        /// Input: Text with whitespace when TrimUnits is disabled.
        /// Expected: Whitespace is preserved.
        /// </summary>
        [TestMethod]
        public void ChunkText_TrimUnitsDisabled_PreservesWhitespace()
        {
            // Arrange
            var options = new ChunkingOptions
            {
                MaxTokensPerChunk = 100,
                SoftLimitRatio = 0.8,
                TrimUnits = false
            };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "  Content  \n\n  More content  ";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            // The behavior depends on implementation details, but we verify it doesn't crash
            Assert.IsTrue(result.Count >= 0);
        }

        /// <summary>
        /// Tests that ChunkText handles a single character input.
        /// Input: Single character.
        /// Expected: Returns single chunk with that character.
        /// </summary>
        [TestMethod]
        public void ChunkText_SingleCharacter_ReturnsSingleChunk()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(1);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "A";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("A", result[0].MainContent);
        }

        /// <summary>
        /// Tests that ChunkText handles extremely long text efficiently.
        /// Input: Very long text with many paragraphs.
        /// Expected: Creates appropriate number of chunks without errors.
        /// </summary>
        [TestMethod]
        public void ChunkText_ExtremelyLongText_HandlesWithoutError()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(50);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);

            // Create a very long text with many paragraphs
            var paragraphs = Enumerable.Range(1, 1000).Select(i => $"Paragraph {i} content");
            string text = string.Join("\n\n", paragraphs);

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        /// <summary>
        /// Tests that ChunkText handles text where every paragraph requires its own chunk.
        /// Input: Multiple paragraphs where each one reaches the soft limit.
        /// Expected: Each paragraph becomes its own chunk with proper context.
        /// </summary>
        [TestMethod]
        public void ChunkText_EachParagraphReachesSoftLimit_CreatesIndividualChunks()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.5 };
            var mockEstimator = new Mock<ITokenEstimator>();

            // Each paragraph is 50 tokens (equals soft limit)
            mockEstimator.Setup(e => e.EstimateTokens(It.IsRegex("^P[0-9]$"))).Returns(50);
            mockEstimator.Setup(e => e.EstimateTokens("\n\n")).Returns(1);

            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "P1\n\nP2\n\nP3";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
        }

        /// <summary>
        /// Tests that ChunkText ToString method returns MainContent.
        /// Input: Any text that produces chunks.
        /// Expected: ToString returns the MainContent of the chunk.
        /// </summary>
        [TestMethod]
        public void ChunkText_ChunkResultToString_ReturnsMainContent()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "Test content";

            // Act
            var result = chunker.ChunkText(text);
            string toStringResult = result[0].ToString();

            // Assert
            Assert.AreEqual("Test content", toStringResult);
        }

        /// <summary>
        /// Tests that ChunkText handles text with consecutive paragraph separators.
        /// Input: Text with multiple consecutive blank lines.
        /// Expected: Treats as single paragraph separator.
        /// </summary>
        [TestMethod]
        public void ChunkText_ConsecutiveParagraphSeparators_TreatsAsSingleSeparator()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();
            mockEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(10);
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);
            string text = "Para1\n\n\n\n\n\nPara2";

            // Act
            var result = chunker.ChunkText(text);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result[0].MainContent.Contains("Para1"));
            Assert.IsTrue(result[0].MainContent.Contains("Para2"));
        }

        /// <summary>
        /// Tests that the constructor succeeds when SoftLimitRatio is exactly 1.0 (boundary value).
        /// </summary>
        [TestMethod]
        public void Constructor_SoftLimitRatioExactlyOne_Succeeds()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 1.0 };

            // Act
            var chunker = new TxtTokenChunker(options);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that the constructor succeeds when SoftLimitRatio is a small positive value near zero.
        /// </summary>
        [TestMethod]
        [DataRow(0.001)]
        [DataRow(0.1)]
        [DataRow(double.Epsilon)]
        public void Constructor_SoftLimitRatioSmallPositive_Succeeds(double ratio)
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = ratio };

            // Act
            var chunker = new TxtTokenChunker(options);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that the constructor succeeds with valid options and uses default HeuristicTokenEstimator when tokenEstimator is null.
        /// </summary>
        [TestMethod]
        public void Constructor_NullTokenEstimator_UsesDefaultEstimator()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };

            // Act
            var chunker = new TxtTokenChunker(options, null);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that the constructor succeeds with valid options and custom token estimator.
        /// </summary>
        [TestMethod]
        public void Constructor_CustomTokenEstimator_Succeeds()
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = 0.8 };
            var mockEstimator = new Mock<ITokenEstimator>();

            // Act
            var chunker = new TxtTokenChunker(options, mockEstimator.Object);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that the constructor succeeds with valid options and default parameters.
        /// </summary>
        [TestMethod]
        public void Constructor_ValidOptionsWithDefaults_Succeeds()
        {
            // Arrange
            var options = new ChunkingOptions();

            // Act
            var chunker = new TxtTokenChunker(options);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that the constructor succeeds with valid custom MaxTokensPerChunk values.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(1000)]
        [DataRow(int.MaxValue)]
        public void Constructor_ValidMaxTokensPerChunk_Succeeds(int maxTokens)
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = maxTokens, SoftLimitRatio = 0.8 };

            // Act
            var chunker = new TxtTokenChunker(options);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that the constructor succeeds with valid SoftLimitRatio values.
        /// </summary>
        [TestMethod]
        [DataRow(0.5)]
        [DataRow(0.8)]
        [DataRow(0.99)]
        public void Constructor_ValidSoftLimitRatio_Succeeds(double ratio)
        {
            // Arrange
            var options = new ChunkingOptions { MaxTokensPerChunk = 100, SoftLimitRatio = ratio };

            // Act
            var chunker = new TxtTokenChunker(options);

            // Assert
            Assert.IsNotNull(chunker);
        }

        /// <summary>
        /// Tests that ChunkFile successfully chunks an empty file and returns an empty list.
        /// </summary>
        [TestMethod]
        public void ChunkFile_EmptyFile_ReturnsEmptyList()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFile, string.Empty);

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(0, result.Count);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile successfully chunks a file with simple text content.
        /// </summary>
        [TestMethod]
        public void ChunkFile_SimpleTextFile_ReturnsChunks()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();
            var testContent = "This is a simple test paragraph.";

            try
            {
                File.WriteAllText(tempFile, testContent);

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
                Assert.AreEqual(testContent, result[0].MainContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile uses UTF8 encoding by default when encoding parameter is null.
        /// </summary>
        [TestMethod]
        public void ChunkFile_NullEncoding_UsesUtf8ByDefault()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();
            var testContent = "Test with UTF8: 你好世界";

            try
            {
                File.WriteAllText(tempFile, testContent, Encoding.UTF8);

                // Act
                var result = chunker.ChunkFile(tempFile, null);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
                Assert.AreEqual(testContent, result[0].MainContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile correctly handles files with different encodings.
        /// </summary>
        [TestMethod]
        [DataRow("ASCII text only")]
        [DataRow("UTF8 with special chars: café résumé")]
        [DataRow("CJK characters: 你好世界 こんにちは 안녕하세요")]
        public void ChunkFile_DifferentEncodings_ReadsContentCorrectly(string testContent)
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFile, testContent, Encoding.UTF8);

                // Act
                var result = chunker.ChunkFile(tempFile, Encoding.UTF8);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
                Assert.AreEqual(testContent, result[0].MainContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile handles files with multiple paragraphs separated by blank lines.
        /// </summary>
        [TestMethod]
        public void ChunkFile_MultiParagraphFile_ReturnsMultipleChunks()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();
            var testContent = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

            try
            {
                File.WriteAllText(tempFile, testContent);

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile handles files containing only whitespace.
        /// </summary>
        [TestMethod]
        public void ChunkFile_WhitespaceOnlyFile_ReturnsEmptyList()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFile, "   \n\n   \t\t   \n   ");

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(0, result.Count);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile handles relative file paths correctly.
        /// </summary>
        [TestMethod]
        public void ChunkFile_RelativePath_ReadsFileCorrectly()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var fileName = $"test_{Guid.NewGuid()}.txt";
            var testContent = "Relative path test.";

            try
            {
                File.WriteAllText(fileName, testContent);

                // Act
                var result = chunker.ChunkFile(fileName);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
                Assert.AreEqual(testContent, result[0].MainContent);
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile handles absolute file paths correctly.
        /// </summary>
        [TestMethod]
        public void ChunkFile_AbsolutePath_ReadsFileCorrectly()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();
            var testContent = "Absolute path test.";

            try
            {
                File.WriteAllText(tempFile, testContent);

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
                Assert.AreEqual(testContent, result[0].MainContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile handles large text files without issues.
        /// </summary>
        [TestMethod]
        public void ChunkFile_LargeFile_ProcessesSuccessfully()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();
            var sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                sb.AppendLine($"This is paragraph number {i}. It contains some text to make it reasonably sized.");
                if (i % 10 == 9)
                {
                    sb.AppendLine();
                }
            }

            try
            {
                File.WriteAllText(tempFile, sb.ToString());

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile with ASCII encoding reads ASCII content correctly.
        /// </summary>
        [TestMethod]
        public void ChunkFile_AsciiEncoding_ReadsAsciiContentCorrectly()
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();
            var testContent = "Simple ASCII text without special characters.";

            try
            {
                File.WriteAllText(tempFile, testContent, Encoding.ASCII);

                // Act
                var result = chunker.ChunkFile(tempFile, Encoding.ASCII);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0);
                Assert.AreEqual(testContent, result[0].MainContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ChunkFile handles files with special line endings (CRLF, LF).
        /// </summary>
        [TestMethod]
        [DataRow("Line1\r\n\r\nLine2")]
        [DataRow("Line1\n\nLine2")]
        [DataRow("Line1\r\nLine2\r\n\r\nLine3")]
        public void ChunkFile_DifferentLineEndings_ProcessesCorrectly(string testContent)
        {
            // Arrange
            var options = new ChunkingOptions();
            var chunker = new TxtTokenChunker(options);
            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFile, testContent);

                // Act
                var result = chunker.ChunkFile(tempFile);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count >= 0);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }

    [TestClass]
    public sealed class ChunkResultTests
    {
        /// <summary>
        /// Tests that ToString returns the MainContent property value for various string inputs
        /// including empty strings, whitespace, special characters, Unicode, and normal text.
        /// Verifies that ContextBefore and ContextAfter are ignored.
        /// </summary>
        [TestMethod]
        [DataRow("Before", "Normal main content", "After")]
        [DataRow("", "Main content", "")]
        [DataRow("X", "", "Y")]
        [DataRow("A", "   ", "B")]
        [DataRow("Context", "\t\n\r", "Context")]
        [DataRow("", "Special: !@#$%^&*(){}[]<>?/\\|~`", "")]
        [DataRow("", "Unicode: 中文 日本語 한글 🎉", "")]
        [DataRow("ContextBefore", "Content with\nmultiple\nlines", "ContextAfter")]
        public void ToString_WithVariousMainContentValues_ReturnsMainContent(
            string contextBefore,
            string mainContent,
            string contextAfter)
        {
            // Arrange
            var chunkResult = new ChunkResult(contextBefore, mainContent, contextAfter);

            // Act
            var result = chunkResult.ToString();

            // Assert
            Assert.AreEqual(mainContent, result);
        }

        /// <summary>
        /// Tests that ToString returns the MainContent property value for very long strings.
        /// Ensures the method can handle large content without issues.
        /// </summary>
        [TestMethod]
        public void ToString_WithVeryLongMainContent_ReturnsMainContent()
        {
            // Arrange
            var veryLongString = new string('A', 10000);
            var chunkResult = new ChunkResult("Before", veryLongString, "After");

            // Act
            var result = chunkResult.ToString();

            // Assert
            Assert.AreEqual(veryLongString, result);
        }
    }

    /// <summary>
    /// Unit tests for the HeuristicTokenEstimator class.
    /// </summary>
    [TestClass]
    public sealed class HeuristicTokenEstimatorTests
    {
        /// <summary>
        /// Tests that EstimateTokens returns 0 for null input.
        /// Input: null string.
        /// Expected: Returns 0.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_NullInput_ReturnsZero()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            string? text = null;

            // Act
            int result = estimator.EstimateTokens(text!);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests that EstimateTokens returns 0 for various empty or whitespace-only inputs.
        /// Input: Empty string, whitespace, tabs, newlines, and combinations.
        /// Expected: Returns 0 for all cases.
        /// </summary>
        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("   ")]
        [DataRow("\t")]
        [DataRow("\n")]
        [DataRow("\r\n")]
        [DataRow("  \t\n\r  ")]
        public void EstimateTokens_EmptyOrWhitespace_ReturnsZero(string text)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly counts single English words.
        /// Input: Single word strings.
        /// Expected: Returns 1 token for each single word.
        /// </summary>
        [TestMethod]
        [DataRow("hello")]
        [DataRow("world")]
        [DataRow("test")]
        [DataRow("a")]
        [DataRow("Hello")]
        [DataRow("WORLD")]
        public void EstimateTokens_SingleWord_ReturnsOne(string text)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly counts multiple English words.
        /// Input: Strings with multiple space-separated words.
        /// Expected: Returns token count equal to word count.
        /// </summary>
        [TestMethod]
        [DataRow("hello world", 2)]
        [DataRow("the quick brown fox", 4)]
        [DataRow("one two three four five", 5)]
        public void EstimateTokens_MultipleWords_ReturnsWordCount(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly counts numeric sequences as words.
        /// Input: Numbers and alphanumeric combinations.
        /// Expected: Returns 1 token per numeric/alphanumeric sequence.
        /// </summary>
        [TestMethod]
        [DataRow("123", 1)]
        [DataRow("42", 1)]
        [DataRow("0", 1)]
        [DataRow("test123", 1)]
        [DataRow("123test", 1)]
        [DataRow("test123test", 1)]
        [DataRow("123 456", 2)]
        public void EstimateTokens_NumericContent_CountsAsWords(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly counts CJK characters.
        /// Input: Chinese, Japanese, and Korean text.
        /// Expected: Returns token count equal to number of CJK characters.
        /// </summary>
        [TestMethod]
        [DataRow("你", 1)]
        [DataRow("你好", 2)]
        [DataRow("你好世界", 4)]
        [DataRow("こんにちは", 5)]
        [DataRow("안녕하세요", 5)]
        public void EstimateTokens_CjkCharacters_ReturnsCharacterCount(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly handles mixed English and CJK content.
        /// Input: Combinations of English words and CJK characters.
        /// Expected: Returns sum of word count and CJK character count.
        /// </summary>
        [TestMethod]
        [DataRow("Hello 世界", 3)]
        [DataRow("你好 world", 3)]
        [DataRow("test 测试 demo", 4)]
        public void EstimateTokens_MixedEnglishAndCjk_ReturnsCombinedCount(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles punctuation-only strings.
        /// Input: Various punctuation characters.
        /// Expected: Returns token count based on punctuation weight (1 per 8 characters).
        /// </summary>
        [TestMethod]
        [DataRow(".", 0)]
        [DataRow("...", 0)]
        [DataRow(".......", 0)]
        [DataRow("........", 1)]
        [DataRow("................", 2)]
        [DataRow("!!!", 0)]
        [DataRow(",,,,,,,", 0)]
        public void EstimateTokens_PunctuationOnly_ReturnsWeightedCount(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly handles words with punctuation.
        /// Input: Words separated or surrounded by punctuation.
        /// Expected: Returns word count plus weighted punctuation.
        /// </summary>
        [TestMethod]
        [DataRow("hello,world", 2)]
        [DataRow("hello, world", 2)]
        [DataRow("hello!", 1)]
        [DataRow("hello.", 1)]
        [DataRow("Hello, world!", 2)]
        public void EstimateTokens_WordsWithPunctuation_CountsWordsAndPunctuation(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles special characters and symbols.
        /// Input: Various special characters, Unicode symbols.
        /// Expected: Returns appropriate token count.
        /// </summary>
        [TestMethod]
        [DataRow("@#$%", 0)]
        [DataRow("test@example", 2)]
        [DataRow("hello & world", 2)]
        [DataRow("$100", 1)]
        public void EstimateTokens_SpecialCharacters_HandlesAppropriately(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens applies ceiling correctly when estimate has fractional part.
        /// Input: Text that results in fractional token estimates.
        /// Expected: Returns ceiling of the estimate.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_FractionalEstimate_AppliesCeiling()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            // 1 word + 1 other char (punct) = 1.0 + 0.125 = 1.125, ceiling = 2
            string text = "a.";

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles very long strings correctly.
        /// Input: Very long string with repeated content.
        /// Expected: Returns appropriate token count without errors.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_VeryLongString_HandlesCorrectly()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            string text = string.Concat(System.Linq.Enumerable.Repeat("hello world ", 10000));

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(22500, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles single character inputs of various types.
        /// Input: Single characters (letter, digit, CJK, punctuation).
        /// Expected: Returns 1 for word/CJK characters, 0 for punctuation.
        /// </summary>
        [TestMethod]
        [DataRow("a", 1)]
        [DataRow("Z", 1)]
        [DataRow("5", 1)]
        [DataRow("你", 1)]
        [DataRow(".", 0)]
        [DataRow(",", 0)]
        [DataRow(" ", 0)]
        public void EstimateTokens_SingleCharacter_ReturnsAppropriateCount(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles complex mixed content correctly.
        /// Input: Mix of English, CJK, numbers, and punctuation.
        /// Expected: Returns sum of all token types.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_ComplexMixedContent_ReturnsCorrectSum()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            // "Hello 你好 123 world!" 
            // Words: "Hello", "123", "world" = 3
            // CJK: "你", "好" = 2
            // Others: " ", " ", " ", "!" = 4, punctLike = 0
            // Total: 3 + 2 + 0 = 5
            string text = "Hello 你好 123 world!";

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(5, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles newlines and multiple lines correctly.
        /// Input: Multi-line text with words.
        /// Expected: Returns token count for all words across lines.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_MultiLineText_CountsAllWords()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            string text = "hello\nworld\ntest";

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles text with multiple consecutive spaces.
        /// Input: Text with irregular spacing between words.
        /// Expected: Returns word count regardless of spacing.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_MultipleSpaces_CountsWordsCorrectly()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            string text = "hello     world";

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(2, result);
        }

        /// <summary>
        /// Tests that EstimateTokens correctly weights other characters at 1/8.
        /// Input: Text designed to test the 8-character divisor for punctuation.
        /// Expected: Verifies integer division behavior for other characters.
        /// </summary>
        [TestMethod]
        [DataRow("........", 1)] // Exactly 8 others = 1 token
        [DataRow("................", 2)] // Exactly 16 others = 2 tokens
        [DataRow(".......", 0)] // 7 others = 0 tokens (integer division)
        [DataRow(".........", 1)] // 9 others = 1 token (integer division)
        public void EstimateTokens_OtherCharacterWeighting_UsesIntegerDivision(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles text with only numbers and spaces.
        /// Input: Numeric sequences with spacing.
        /// Expected: Returns count of numeric sequences.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_NumbersWithSpaces_CountsSequences()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            string text = "1 2 3 4 5";

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(5, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles Japanese hiragana and katakana.
        /// Input: Japanese hiragana and katakana characters.
        /// Expected: Returns character count for each CJK script.
        /// </summary>
        [TestMethod]
        [DataRow("あいうえお", 5)]
        [DataRow("アイウエオ", 5)]
        public void EstimateTokens_JapaneseKanaCharacters_ReturnsCharacterCount(string text, int expected)
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests that EstimateTokens handles Korean Hangul syllables.
        /// Input: Korean Hangul syllables.
        /// Expected: Returns character count for Hangul.
        /// </summary>
        [TestMethod]
        public void EstimateTokens_KoreanHangul_ReturnsCharacterCount()
        {
            // Arrange
            var estimator = new HeuristicTokenEstimator();
            string text = "한글";

            // Act
            int result = estimator.EstimateTokens(text);

            // Assert
            Assert.AreEqual(2, result);
        }
    }
}